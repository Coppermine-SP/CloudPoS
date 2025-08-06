using System.Globalization;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace CloudInteractive.CloudPos.Pages.Administrative;

public partial class Statistics(IDbContextFactory<ServerDbContext> factory) : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    private IJSObjectReference? _chartModule;
    private DotNetObjectReference<Statistics>? _dotNetRef;
    
    private bool _isLoading = true;
    private bool _isReadyToRenderCharts;
    
    private DateTime _selectedMonth;
    private int _selectedWeekIndex;
    private DateTime? _selectedDate;
    private readonly CultureInfo _koCulture = new("ko-KR");
    
    enum OptionContent {Daily, Monthly}
    private OptionContent _selectedOption = OptionContent.Daily;
    
    private string WeekDateRangeLabel => _weeksInMonth.Count != 0 ? 
        $"{_weeksInMonth[_selectedWeekIndex].First():d} ~ {_weeksInMonth[_selectedWeekIndex].Last():d}" : "";
    private bool HasSalesDataForCurrentWeek => _weeklySalesData.Sum(d => d.Amount) > 0;
 
    // 데이터 저장 변수
    private List<Order> _monthOrders = [];
    private List<DailySalesData> _weeklySalesData = [];
    private List<ItemInfo> _currentStatistics = [];
    private readonly List<List<DateTime>> _weeksInMonth = [];
    // 데이터 모델
    public record DailySalesData(string Label, int Amount, DateTime Date);
    public record ItemInfo(string ItemName, int Quantity, int Price);
    
    protected override async Task OnInitializedAsync()
    {
        _dotNetRef = DotNetObjectReference.Create(this);
        _selectedMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        await LoadDataMonthAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            _chartModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "/Pages/Administrative/Statistics.razor.js");
        if (_isReadyToRenderCharts && _chartModule is not null)
        {
            _isReadyToRenderCharts = false;
            await RenderSalesChartAsync();
        }
    }

    private async Task ChangeMonth(int monthsToAdd)
    {
        _selectedMonth = _selectedMonth.AddMonths(monthsToAdd);
        await LoadDataMonthAsync();
    }

    private async Task ChangeWeek(int direction)
    {
        _selectedWeekIndex += direction;
        _isReadyToRenderCharts = true;
        UpdateWeeklyChartData();
        await RenderSalesChartAsync();
    }
    
    private async Task LoadDataMonthAsync()
    {
        _isLoading = true;
        _isReadyToRenderCharts = false;
        await InvokeAsync(StateHasChanged);

        await using var context = await factory.CreateDbContextAsync();
        var startDate = _selectedMonth;
        var endDate = _selectedMonth.AddMonths(1);
        
        _monthOrders = await context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Item)
            .Where(o => o.Status == Order.OrderStatus.Completed && o.CreatedAt >= startDate && o.CreatedAt < endDate)
            .ToListAsync();
        
        _selectedWeekIndex = 0;
        _selectedDate= null;
        
        GenerateWeeksForMonth();
        UpdateWeeklyChartData();
        UpdateStatistics();

        _isLoading = false;
        _isReadyToRenderCharts = true;
        await InvokeAsync(StateHasChanged);
        await RenderSalesChartAsync();
    }
    
    private void UpdateWeeklyChartData()
    {
        if (_weeksInMonth.Count == 0) return;

        var currentWeekDays = _weeksInMonth[_selectedWeekIndex];
        var salesByDate = _monthOrders
            .Where(o => currentWeekDays.Contains(o.CreatedAt.Date))
            .SelectMany(o => o.OrderItems)
            .GroupBy(oi => oi.Order.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Sum(oi => oi.Quantity * oi.Item.Price));

        _weeklySalesData = currentWeekDays
            .Select(day => new DailySalesData(
                $"{day:yyyy-MM-dd} ({day.ToString("ddd", _koCulture)})",
                salesByDate.GetValueOrDefault(day, 0),
                day
            )).ToList();
    }
    
    private void UpdateStatistics()
    {
        // 보여줄 통계가 없으면 종료
        if (_selectedOption == OptionContent.Daily && _selectedDate is null)
        {
            _currentStatistics = [];
            return;
        }
        var sourceData = _monthOrders;

        // 특정 날짜가 선택되었다면, 해당 날짜의 주문만 필터링
        if (_selectedOption != OptionContent.Monthly && _selectedDate.HasValue)
        {
            sourceData = _monthOrders
                .Where(o => o.CreatedAt.Date == _selectedDate.Value.Date)
                .ToList();
        }

        _currentStatistics = sourceData
            .SelectMany(o => o.OrderItems)
            .GroupBy(oi => oi.Item.Name)
            .Select(g => new ItemInfo(g.Key, g.Sum(oi => oi.Quantity), g.First().Item.Price))
            .OrderByDescending(m => m.Quantity)
            .ToList();
    }
    
    private void GenerateWeeksForMonth()
    {
        _weeksInMonth.Clear();
        var startDate = _selectedMonth;
        var endDate = _selectedMonth.AddMonths(1).AddDays(-1);
        
        var currentDay = startDate;
        while(currentDay <= endDate)
        {
            var week = new List<DateTime>();
            int startOfWeek = (int)currentDay.DayOfWeek == 0 ? 6 : (int)currentDay.DayOfWeek - 1;
            
            for (int i = startOfWeek; i < 7 && currentDay <= endDate; i++)
            {
                week.Add(currentDay.Date);
                currentDay = currentDay.AddDays(1);
            }
            _weeksInMonth.Add(week);
        }
    }
    
    private async Task RenderSalesChartAsync()
    {
        if (_chartModule is null) return;
        var chartData = new {
            labels = _weeklySalesData.Select(d => d.Label),
            datasets = new[] { new {
                label = "판매 금액", 
                data = _weeklySalesData.Select(d => d.Amount),
                fill = true,
                borderColor = "rgba(13, 110, 253, 0.6)",
                backgroundColor = "rgb(13, 110, 253)"
            }}
        };
        await _chartModule.InvokeVoidAsync("renderVerticalBarChart", _dotNetRef, "salesChart", chartData);
    }
    
    // JavaScript에서 차트 클릭 시 호출할 메서드
    [JSInvokable]
    public void HandleChartClick(int clickedIndex)
    {
        if (clickedIndex >= 0 && clickedIndex < _weeklySalesData.Count)
        {
            _selectedOption = OptionContent.Daily;
            _selectedDate = _weeklySalesData[clickedIndex].Date;
            UpdateStatistics();
            InvokeAsync(StateHasChanged);
        }
    }
    
    private async Task ShowStatistics(OptionContent select)
    {
        _selectedOption = select;
        UpdateStatistics();
        await InvokeAsync(StateHasChanged);
    }
  
    private string GetRankingTitle() => _selectedDate.HasValue && _selectedOption == OptionContent.Daily ? 
        _selectedDate.Value.ToString("M월 d일 통계") : 
        _selectedMonth.ToString("M월") + " 전체 통계";
    
    private bool IsNextMonthDisabled() => 
        _selectedMonth.Year == DateTime.Now.Year && _selectedMonth.Month == DateTime.Now.Month;
        
    public async ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();
        if (_chartModule is not null)
        {
            await _chartModule.DisposeAsync();
        }
    }
}