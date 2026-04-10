using CarRentalSystem_API.DTO.PromotionDTO;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers.AdminControllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Tags("Admin Promotion Management")]
    public class PromotionManagementController : Controller
    {
        private readonly AppDbContext _db;
        public PromotionManagementController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet("promotions")]
        public async Task<IActionResult> GetAllPromotions()
        {
            var promotions = await _db.Promotions.ToListAsync();
            return Ok(promotions);
        }
        [HttpPost("createpromotion")]
        public async Task<IActionResult> CreatePromotion([FromBody] CreatePromotionDTO createPromotion)
        {
            var existingPromotion = await _db.Promotions.AnyAsync(p => p.PromotionCode == createPromotion.PromotionCode);
            if (existingPromotion)
                return BadRequest(new
                {
                    error = "Duplicate Promotion Code",
                    message = "Promotion code already exists. Please choose a unique promotion code."
                });
            if (createPromotion.StartDate >= createPromotion.EndDate)
                return BadRequest(new
                {
                    error = "Invalid Date Range",
                    message = "Start date must be before end date."
                });
            if (createPromotion.DiscountPercentage <= 0 || createPromotion.DiscountPercentage > 100)
                return BadRequest(new
                {
                    error = "Invalid Discount Percentage",
                    message = "Discount percentage must be between 0 and 100."
                });
            if (createPromotion.MaxDiscountAmount != null)
            {
                if (createPromotion.MaxDiscountAmount <= 0)
                    return BadRequest(new
                    {
                        error = "Invalid Max Discount Amount",
                        message = "Max discount amount must be greater than 0."
                    });
            }
            else
                createPromotion.MaxDiscountAmount = null;

            var promotion = new Promotion
            {
                Name = createPromotion.Name,
                PromotionCode = createPromotion.PromotionCode,
                DiscountPercentage = createPromotion.DiscountPercentage, // 0.15m for 15% discount < This is simple
                MaxDiscountAmount = createPromotion.MaxDiscountAmount,
                StartDate = createPromotion.StartDate,
                EndDate = createPromotion.EndDate,
                IsActive = true,
                PromotionScope = string.IsNullOrEmpty(createPromotion.PromotionScope) ? "All" : createPromotion.PromotionScope,
                TargetValue = string.IsNullOrEmpty(createPromotion.TargetValue) ? null : createPromotion.TargetValue
            };
            _db.Promotions.Add(promotion);
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "Promotion created successfully.",
                PromotionID = promotion.PromotionID
            });
        }
        [HttpPost("updatepromotion")]
        public async Task<IActionResult> UpdatePromotion([FromBody] UpdatePromotionDTO updatePromotion)
        {
            var existingPromotion = await _db.Promotions.FirstOrDefaultAsync(p => p.PromotionID == updatePromotion.PromotionID);
            if (existingPromotion == null)
                return NotFound(new
                {
                    error = "Promotion Not Found",
                    message = $"Promotion with ID {updatePromotion.PromotionID} not found."
                });
            if (updatePromotion.StartDate >= updatePromotion.EndDate)
                return BadRequest(new
                {
                    error = "Invalid Date Range",
                    message = "Start date must be before end date."
                });
            if (updatePromotion.DiscountPercentage <= 0 || updatePromotion.DiscountPercentage > 100)
                return BadRequest(new
                {
                    error = "Invalid Discount Percentage",
                    message = "Discount percentage must be between 0 and 100."
                });
            if (updatePromotion.MaxDiscountAmount != null)
                if (updatePromotion.MaxDiscountAmount <= 0)
                    return BadRequest(new
                    {
                        error = "Invalid Max Discount Amount",
                        message = "Max discount amount must be greater than 0."
                    });

            _db.Entry(existingPromotion).CurrentValues.SetValues(updatePromotion);
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "Promotion updated successfully.",
                PromotionID = existingPromotion.PromotionID
            });
        }
      
        [HttpPost("togglepromotionstatus/{id}")]
        public async Task<IActionResult> TogglePromotionStatus(int id)
        {
            var existingPromotion = await _db.Promotions.FirstOrDefaultAsync(p => p.PromotionID == id);
            if (existingPromotion == null)
                return NotFound($"Promotion with ID {id} not found.");
            existingPromotion.IsActive = !existingPromotion.IsActive;
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = $"Promotion {(existingPromotion.IsActive ? "activated" : "deactivated")} successfully.",
                PromotionID = id,
                IsActive = existingPromotion.IsActive
            });
        }
    }
}
