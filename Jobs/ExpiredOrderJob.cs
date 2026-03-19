using BE.Models;
using BE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BE.Jobs;

public class ExpiredOrderJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredOrderJob> _logger;

    public ExpiredOrderJob(IServiceScopeFactory scopeFactory, ILogger<ExpiredOrderJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ShopQuanAoContext>();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

                // Tìm đơn VNPay quá hạn: status = 0 (chờ thanh toán) + đã hết hạn
                var expiredOrders = await context.Orders
                    .Where(o => o.Status == 0 && o.PaymentExpireAt != null && o.PaymentExpireAt < DateTime.Now)
                    .ToListAsync(stoppingToken);

                foreach (var order in expiredOrders)
                {
                    await orderService.CancelOrder(order);
                    _logger.LogInformation("Đã hủy đơn hàng #{OrderId} do hết hạn thanh toán", order.OrderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý đơn hàng quá hạn");
            }

            // Chờ 5 phút rồi chạy lại
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
