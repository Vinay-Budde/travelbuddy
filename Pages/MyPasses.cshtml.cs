using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TravelBuddy.Data;
using TravelBuddy.Models;
using TravelBuddy.Services;

namespace TravelBuddy.Pages
{
    public class MyPassesModel : PageModel
    {
        private readonly TravelBuddyDbContext _context;
        private readonly IPassService _passService;

        public MyPassesModel(TravelBuddyDbContext context, IPassService passService)
        {
            _context = context;
            _passService = passService;
        }

        public IList<Pass> Passes { get; set; } = new List<Pass>();
        public Dictionary<long, bool> EligibilityMap { get; set; } = new();

        public async Task OnGetAsync()
        {
            var demoUser = await _context.Users.FirstOrDefaultAsync();
            if (demoUser == null) return;

            Passes = await _context.Passes
                .Where(p => p.UserId == demoUser.Id)
                .Include(p => p.OrderItem)
                .OrderByDescending(p => p.IssuedAtUtc)
                .ToListAsync();

            foreach (var pass in Passes)
            {
                var eligible = await _passService.CanCancelPassAsync(pass.PassId, demoUser.Id);
                EligibilityMap[pass.PassId] = eligible;
            }
        }
    }
}
