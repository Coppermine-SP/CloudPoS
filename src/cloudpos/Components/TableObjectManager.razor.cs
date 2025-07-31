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
    
    private bool _isModify = false;
    private IJSObjectReference? _jsModule; 
    private DotNetObjectReference<TableObjectManager>? _dotNetObjectReference;
    
    protected override async Task OnInitializedAsync()
    { 
        await LoadTablesAsync(); 
    }
    
    private async Task LoadTablesAsync()
    { 
        await using var context = await factory.CreateDbContextAsync(); 
        _allTables = await context.Tables.Include(t => t.Cell).ToListAsync();
        _isModify = false;
        StateHasChanged();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetObjectReference = DotNetObjectReference.Create(this);
            _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/tableManager.js");
        }
        if (_jsModule != null)
        {
            await _jsModule.InvokeVoidAsync("initializeDragAndDrop", _dotNetObjectReference);
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

    private async Task DeleteTableAsync(int tableId)
    {
        await using var context = await factory.CreateDbContextAsync(); 
        bool isReferenced = await context.Set<TableSession>().AnyAsync(ts => ts.TableId == tableId);
        if (isReferenced)
        {
            await modal.ShowAsync<AlertModal, bool>("삭제 불가", ModalService.Params().Add("InnerHtml", "이 테이블은 현재 사용중인 세션이 있어 삭제할 수 없습니다.<br>관련된 모든 주문 내역 또는 세션을 먼저 삭제해야 합니다.").Build());
            return;
        }
        if (await modal.ShowAsync<AlertModal, bool>("테이블 삭제", ModalService.Params().Add("InnerHtml", "정말 이 테이블을 삭제하시겠습니까?<br><br><strong>이 작업은 되돌릴 수 없습니다.</strong>").Add("IsCancelable", true).Build()))
        {
            try
            { 
                await context.Tables.Where(t => t.TableId == tableId).ExecuteDeleteAsync(); 
                await LoadTablesAsync();
            }
            catch (Exception e)
            { 
                DbSaveChangesErrorHandler(e);
            }
        }
    }
    [JSInvokable]
    public void UpdateTableState(int tableId, int newX, int newY, string targetContainer) 
    { 
        var draggedTable = _allTables.FirstOrDefault(t => t.TableId == tableId);
        if (draggedTable == null) return;

        // 테이블이 그리드에 놓인 경우
        if (targetContainer == "grid")
        {
            var tableAtTarget = _allTables.FirstOrDefault(t => t.Cell != null && t.Cell.X == newX && t.Cell.Y == newY);
            
            // 겹치는 경우 종료
            if (tableAtTarget != null) return;
            
            if (draggedTable.Cell == null)
            {
                draggedTable.Cell = new TableViewCell();
            }
            draggedTable.Cell.X = newX;
            draggedTable.Cell.Y = newY;
        }
        // 테이블이 미배치 목록으로 돌아간 경우
        else 
        { 
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
        logger.LogError(e, "데이터베이스 변경 사항 저장 중 오류 발생");
        _ = interop.ShowNotifyAsync("서버 오류가 발생하여 변경 사항을 저장할 수 없었습니다.", InteractiveInteropService.NotifyType.Error);
    }
    
    public void Dispose()
    { 
        _dotNetObjectReference?.Dispose();
    }
}