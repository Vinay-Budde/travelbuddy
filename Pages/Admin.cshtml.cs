using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelBuddy.Data;
using TravelBuddy.Models;

namespace TravelBuddy.Pages
{
    public class AdminModel : PageModel
    {
        private readonly TravelBuddyDbContext _context;

        public AdminModel(TravelBuddyDbContext context)
        {
            _context = context;
        }

        public decimal TotalRevenue { get; set; }
        public int ActivePassesCount { get; set; }
        public int ValidationsCount { get; set; }
        public decimal TotalRefunds { get; set; }

        public IList<Pass> Passes { get; set; } = new List<Pass>();
        public IList<Order> Orders { get; set; } = new List<Order>();
        public IList<Refund> Refunds { get; set; } = new List<Refund>();
        public IList<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        public async Task OnGetAsync()
        {
            TotalRevenue = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Succeeded)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            TotalRefunds = await _context.Refunds
                .Where(r => r.Status == RefundStatus.Completed)
                .SumAsync(r => (decimal?)r.RefundAmount) ?? 0;

            ActivePassesCount = await _context.Passes.CountAsync(p => p.Status == PassStatus.Active);
            ValidationsCount = await _context.BoardingValidations.CountAsync();

            Passes = await _context.Passes
                .Include(p => p.User)
                .OrderByDescending(p => p.IssuedAtUtc)
                .Take(20)
                .ToListAsync();

            Orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAtUtc)
                .Take(20)
                .ToListAsync();

            Refunds = await _context.Refunds
                .OrderByDescending(r => r.RequestedAtUtc)
                .Take(20)
                .ToListAsync();

            AuditLogs = await _context.AuditLogs
                .OrderByDescending(a => a.CreatedAtUtc)
                .Take(20)
                .ToListAsync();
        }
    }
}
