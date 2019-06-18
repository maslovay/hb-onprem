using HBData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class Createviews2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
               string script =
        @"
        CREATE VIEW public.Test
        AS SELECT *
        FROM public.AspNetUsers";
        RecordsContext ctx = new RecordsContext();
        ctx.Database.ExecuteSqlCommand(script);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RecordsContext ctx = new RecordsContext();
            ctx.Database.ExecuteSqlCommand("DROP VIEW public.Test");
        }
    }
}
