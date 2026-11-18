using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelBuddy.Data;
using TravelBuddy.Models;
using ModelRoute = TravelBuddy.Models.Route;

namespace TravelBuddy.Pages
{
    public class IndexModel : PageModel
    {
        private readonly TravelBuddyDbContext _context;

        public IndexModel(TravelBuddyDbContext context)
        {
            _context = context;
        }

        public IList<FareProduct> FareProducts { get; set; } = new List<FareProduct>();
        public IList<ModelRoute> PublishedRoutes { get; set; } = new List<ModelRoute>();

        public async Task OnGetAsync()
        {
            FareProducts = await _context.FareProducts
                .Where(f => f.Status == FareProductStatus.Published)
                .ToListAsync();

            PublishedRoutes = await _context.Routes
                .Where(r => r.Status == "Published")
                .Include(r => r.RouteStops)
                .ThenInclude(rs => rs.Stop)
                .ToListAsync();
        }
    }
}
