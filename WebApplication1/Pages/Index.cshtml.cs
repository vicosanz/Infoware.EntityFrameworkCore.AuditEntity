using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Test;
using Test.Models;

namespace WebApplication1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly BlogContext _blogContext;

        public IndexModel(ILogger<IndexModel> logger, BlogContext blogContext)
        {
            _logger = logger;
            _blogContext = blogContext;
        }

        public async void OnGet()
        {
            _blogContext.Add(new Blog()
            {
                Name = "Test",
            });
            await _blogContext.SaveChangesAsync();
        }
    }
}