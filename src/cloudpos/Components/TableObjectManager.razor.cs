using CloudInteractive.CloudPos.Components.Modal;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace CloudInteractive.CloudPos.Components;

public partial class TableObjectManager (
    IDbContextFactory<ServerDbContext> factory, 
    ModalService modal, 
    InteractiveInteropService interop, 
    ILogger<TableObjectManager> logger) : ComponentBase, IDisposable
{ 
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    
    private List<Table> _allTables = [];
    private List<Table> UnplacedTables => _allTables.Where(t => t.Cell == null).OrderBy(t => t.Name).ToList();
    private List<Table> PlacedTables => _allTables.Where(t => t.Cell != null).ToList();

    private string? _newTableName;

    private bool _isModify;
    private IJSObjectReference? _jsModule; 
    private DotNetObjectReference<TableObjectManager>? _dotNetObjectReference;
    
    public enum TableManageAction { None, Rename, Delete, Cancel }

    public sealed record TableManageResult(
        TableManageAction Action,
        string? NewName
    );
    
    protected override async Task OnInitializedAsync()
    { 
        await LoadTablesAsync(); 
    }
    
    private async Task LoadTablesAsync()
    { 
        await using var context = await factory.CreateDbContextAsync(); 
        _allTables = await context.Tables.Include(t => t.Cell).Include(t => t.Sessions).ToListAsync();
        _isModify = false;
        StateHasChanged();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (firstRender)
            {
                _dotNetObjectReference = DotNetObjectReference.Create(this);
                _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/TableObjectManager.razor.js");
                if (_jsModule != null)
                {
                    await _jsModule.InvokeVoidAsync("initializeDragAndDrop", _dotNetObjectReference);
                }
            }
        }
        catch
        {
            //ignored
        }
    }
    private async Task CreateTableAsync()
    {
        if (string.IsNullOrWhiteSpace(_newTableName)) return;
        await using var context = await factory.CreateDbContextAsync();
        var newTable = new Table { Name = _newTableName };
        context.Tables.Add(newTable);
        try
        {
            await context.SaveChangesAsync(); 
            _newTableName = string.Empty;
            await LoadTablesAsync();
        }
        catch (Exception e) {
            DbSaveChangesErrorHandler(e);
        }
    }
    
    private async Task CancelChanges() 
    {
        await LoadTablesAsync();
    }
    private async Task ModifyTableNameAsync(int tableId, string? tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            await interop.ShowNotifyAsync("테이블 이름을 입력하세요.", InteractiveInteropService.NotifyType.Warning);
            return;
        }

        var newName = tableName.Trim();
        if (newName.Length > 30)
            newName = newName[..30];

        await using var context = await factory.CreateDbContextAsync();

        try
        {
            // 중복 이름 방지
            var exists = await context.Tables
                .AnyAsync(t => t.TableId != tableId && t.Name == newName);
            if (exists)
            {
                await interop.ShowNotifyAsync("이미 존재하는 이름입니다.", InteractiveInteropService.NotifyType.Error);
                return;
            }
            
            var updated = await context.Tables
                .Where(t => t.TableId == tableId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(t => t.Name, newName));

            if (updated == 0)
            {
                await interop.ShowNotifyAsync("대상 테이블을 찾지 못했습니다.", InteractiveInteropService.NotifyType.Error);
                return;
            }

            await interop.ShowNotifyAsync("테이블 이름이 변경되었습니다.", InteractiveInteropService.NotifyType.Success);
            await LoadTablesAsync();
        }
        catch (Exception e)
        {
            DbSaveChangesErrorHandler(e);
        }
    }

    private async Task ManageTableAsync(int tableId, string currentName)
    {
        await using var context = await factory.CreateDbContextAsync();
        bool isReferenced = await context.Set<TableSession>().AnyAsync(ts => ts.TableId == tableId);
        
        var result = await modal.ShowAsync<TableManageModal, TableManageResult>(
            "테이블 관리",
            ModalService.Params()
                .Add("TableId", tableId)
                .Add("CurrentName", currentName)
                .Add("IsReferenced", isReferenced)
                .Build());

        if (result is null || result.Action is TableManageAction.Cancel or TableManageAction.None)
            return;

        switch (result.Action)
        {
            case TableManageAction.Rename:
            {
                await ModifyTableNameAsync(tableId, result.NewName);
                break;
            }

            case TableManageAction.Delete:
            {
                try
                {
                    await context.Tables.Where(t => t.TableId == tableId).ExecuteDeleteAsync();
                    await interop.ShowNotifyAsync("삭제되었습니다.", InteractiveInteropService.NotifyType.Success);
                    await LoadTablesAsync();
                }
                catch (Exception e)
                {
                    DbSaveChangesErrorHandler(e);
                }
                break;
            }
        }
    }

    [JSInvokable]
    public async Task UpdateTableState(int tableId, int newX, int newY, string targetContainer) 
    { 
        var draggedTable = _allTables.FirstOrDefault(t => t.TableId == tableId);
        if (draggedTable == null) return;

        // 테이블이 그리드에 놓인 경우
        if (targetContainer == "grid")
        {
            var tableAtTarget = _allTables.FirstOrDefault(t => t.Cell != null && t.Cell.X == newX && t.Cell.Y == newY);
            
            // 겹치는 경우 종료
            if (tableAtTarget != null)
            {
                _ = interop.ShowNotifyAsync("해당 위치에는 이미 다른 테이블이 있습니다.", InteractiveInteropService.NotifyType.Warning);
                await RefreshAsync();
                return;
            }
            
            draggedTable.Cell ??= new TableViewCell();
            draggedTable.Cell.X = newX;
            draggedTable.Cell.Y = newY;
        }
        // 테이블이 미배치 목록으로 돌아간 경우
        else 
        { 
            // 테이블에 세션이 할당 된 경우 미배치 목록으로 못 가게 막기
            if (draggedTable.Sessions.Any(x => x.State == TableSession.SessionState.Active))
            {
                _ = interop.ShowNotifyAsync("테이블에 세션이 할당 되어 있어 미배치 목록으로 돌아갈 수 없습니다.", InteractiveInteropService.NotifyType.Error);
                _allTables = new List<Table>(_allTables);
                await RefreshAsync();
                return;
            }
            draggedTable.Cell = null;
        }
        _isModify = true;
        StateHasChanged();
    }
    
    
    private async Task SaveChanges()
    { 
        await using var context = await factory.CreateDbContextAsync(); 
        await using var transaction = await context.Database.BeginTransactionAsync();
        
        try 
        { 
            var tablesInDb = await context.Tables.Include(t => t.Cell).Where(t => _allTables.Select(at => at.TableId).Contains(t.TableId)).ToListAsync();

            foreach (var tableInDb in tablesInDb) 
            { 
                var clientTable = _allTables.FirstOrDefault(t => t.TableId == tableInDb.TableId);
                if (clientTable == null) continue;
                
                // Case 1: 테이블이 배치된 경우
                if (clientTable.Cell != null)
                {
                    if (tableInDb.Cell == null || tableInDb.Cell.X != clientTable.Cell.X || tableInDb.Cell.Y != clientTable.Cell.Y) 
                    { 
                        if (tableInDb.Cell != null) context.Remove(tableInDb.Cell);
                        tableInDb.Cell = new TableViewCell { X = clientTable.Cell.X, Y = clientTable.Cell.Y };
                    }
                }
                // Case 2: 테이블이 미배치된 경우
                   else
                {
                    if (tableInDb.Cell != null)
                    { 
                        context.Remove(tableInDb.Cell);
                        tableInDb.Cell = null;
                    }
                }
            }
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            await interop.ShowNotifyAsync("레이아웃이 저장되었습니다.", InteractiveInteropService.NotifyType.Success);
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            DbSaveChangesErrorHandler(e);
        }
        finally
        {
            await LoadTablesAsync();
        }
    }

    private void DbSaveChangesErrorHandler(Exception e)
    {
        logger.LogError(e.ToString());
        _ = interop.ShowNotifyAsync("서버 오류가 발생하여 변경 사항을 저장할 수 없었습니다.", InteractiveInteropService.NotifyType.Error);
    }
    
    public void Dispose()
    { 
        _dotNetObjectReference?.Dispose();
    }
    // UI를 비동기 새로고침 하는 메서드
    private async Task RefreshAsync()
    {
        var originalTables = new List<Table>(_allTables);
        _allTables.Clear();
        
        StateHasChanged();
        await Task.Yield();
        
        _allTables = originalTables;
        StateHasChanged();
    }
}