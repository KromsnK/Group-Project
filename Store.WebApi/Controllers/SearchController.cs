using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store.Data;
using Store.Data.Entities;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Store.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SearchController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> SearchProducts([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { message = "Пошуковий запит не може бути порожнім." });
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdClaim, out int userId))
                {
                    var searchRecord = new SearchHistory
                    {
                        SearchQuery = query.Trim(),
                        SearchDate = DateTime.UtcNow,
                        UserId = userId
                    };

                    _context.SearchHistories.Add(searchRecord);
                    await _context.SaveChangesAsync();
                }
            }

            var products = await _context.Products
                .Where(p => p.Name.Contains(query))
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price
                })
                .ToListAsync();

            return Ok(products);
        }

        [Authorize]
        [HttpGet("my-history")]
        public async Task<IActionResult> GetMyHistory()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var history = await _context.SearchHistories
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.SearchDate)
                .Take(10)
                .Select(h => h.SearchQuery)
                .Distinct()
                .ToListAsync();

            return Ok(history);
        }
    }
}
