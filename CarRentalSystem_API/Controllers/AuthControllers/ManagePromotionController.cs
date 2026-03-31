using CarRentalSystem_API.DTO.PromotionDTO;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers.AuthControllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ManagePromotionController : Controller
    {
        private readonly AppDbContext _db;
        public ManagePromotionController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllPromotions()
        {
            var promotions = await _db.Promotions.ToListAsync();
            return Ok(promotions);
        }
       
        [HttpPost]
        public async Task<IActionResult> CreatePromotion([FromBody] CreatePromotionDTO createPromotion)
        {
            var existingPromotion = await _db.Promotions.AnyAsync(p => p.PromotionCode == createPromotion.PromotionCode);
            if (existingPromotion)
                return BadRequest("Promotion code already exists.");
            if (createPromotion.StartDate >= createPromotion.EndDate)
                return BadRequest("Start date must be before end date.");
            if (createPromotion.DiscountPercentage <= 0 || createPromotion.DiscountPercentage > 100)
                return BadRequest("Discount percentage must be between 0 and 100.");
            var promotion = new Promotion
            {
                Name = createPromotion.Name,
                PromotionCode = createPromotion.PromotionCode,
                DiscountPercentage = createPromotion.DiscountPercentage,
                MaxDiscountAmount = createPromotion.MaxDiscountAmount,
                StartDate = createPromotion.StartDate,
                EndDate = createPromotion.EndDate,
                IsActive = true
            };
        }
    }
}
