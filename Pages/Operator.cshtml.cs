using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelBuddy.Data;
using TravelBuddy.Models;
using ModelRoute = TravelBuddy.Models.Route;

namespace TravelBuddy.Pages
{
    public class OperatorModel : PageModel
    {
        private readonly TravelBuddyDbContext _context;

        public OperatorModel(TravelBuddyDbContext context)
        {
            _context = context;
        }

        public IList<ModelRoute> Routes { get; set; } = new List<ModelRoute>();
        public IList<Pass> RecentPasses { get; set; } = new List<Pass>();
        public IList<BoardingValidation> RecentLogs { get; set; } = new List<BoardingValidation>();

        public async Task OnGetAsync()
        {
            Routes = await _context.Routes.Where(r => r.Status == "Published").ToListAsync();

            RecentPasses = await _context.Passes
                .Where(p => p.Status == PassStatus.Active)
                .OrderByDescending(p => p.IssuedAtUtc)
                .Take(5)
                .ToListAsync();

            RecentLogs = await _context.BoardingValidations
                .Include(b => b.Pass)
                .Include(b => b.Route)
                .OrderByDescending(b => b.ValidatedAtUtc)
                .Take(10)
                .ToListAsync();
        }
    }
}
