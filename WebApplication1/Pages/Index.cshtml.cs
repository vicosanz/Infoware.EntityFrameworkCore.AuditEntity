﻿using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebApplication1.Models;

namespace WebApplication1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly BlogContext _blogContext;

        public IndexModel(BlogContext blogContext)
        {
            _blogContext = blogContext;
        }

        public async void OnGet()
        {
            var migrator = _blogContext.Database.GetService<IMigrator>();
            await migrator.MigrateAsync();

            _blogContext.Add(new Blog()
            {
                Name = "Test",
            });

            var blog = _blogContext.Blogs.Single(x => x.Id == 1);
            if (blog != null)
            {
                blog.Name = "Test2";
            }
            await _blogContext.SaveChangesAsync();
        }
    }
}