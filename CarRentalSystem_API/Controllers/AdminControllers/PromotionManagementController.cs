using CarRentalSystem_API.DTO.PromotionDTO;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers.AdminControllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PromotionManagementController : Controller
    {
        private readonly AppDbContext _db;
        public PromotionManagementController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet]
        public async Task<IActionResult> GetactivePromtions()
        {
            var promotions = await _db.Promotions
                .Where(p => p.IsActive && p.StartDate <= DateTime.UtcNow && p.EndDate >= DateTime.UtcNow)
                .ToListAsync();
            return Ok(promotions);
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
            _db.Promotions.Add(promotion);
            await _db.SaveChangesAsync();
            return Ok(new
            {
                Message = "Promotion created successfully.",
                PromotionID = promotion.PromotionID
            });
        }
        [HttpPost]
        public async Task<IActionResult> UpdatePromotion([FromBody] UpdatePromotionDTO updatePromotion)
        {
            var existingPromotion = await _db.Promotions.FirstOrDefaultAsync(p => p.PromotionID == updatePromotion.PromotionID);
            if (existingPromotion == null)
                return NotFound($"Promotion with ID {updatePromotion.PromotionID} not found.");
            if (updatePromotion.StartDate >= updatePromotion.EndDate)
                return BadRequest("Start date must be before end date.");
            if (updatePromotion.DiscountPercentage <= 0 || updatePromotion.DiscountPercentage > 100)
                return BadRequest("Discount percentage must be between 0 and 100.");

            _db.Entry(existingPromotion).CurrentValues.SetValues(updatePromotion);
            await _db.SaveChangesAsync();
            return Ok(new
            {
                Message = "Promotion updated successfully.",
                PromotionID = existingPromotion.PromotionID
            });
        }
        [HttpDelete("{id}/delete")]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            var existingPromotion = await _db.Promotions.FirstOrDefaultAsync(p => p.PromotionID == id);
            if (existingPromotion == null)
                return NotFound($"Promotion with ID {id} not found.");
            if (existingPromotion.StartDate <= DateTime.Now && existingPromotion.EndDate >= DateTime.Now)
                return BadRequest("Cannot delete an active promotion.");
            _db.Promotions.Remove(existingPromotion);
            await _db.SaveChangesAsync();
            return Ok(new
            {
                Message = "Promotion deleted successfully.",
                PromotionID = id
            });
        }
        [HttpPost("{id}/toggle-status")]
        public async Task<IActionResult> TogglePromotionStatus(int id)
        {
            var existingPromotion = await _db.Promotions.FirstOrDefaultAsync(p => p.PromotionID == id);
            if (existingPromotion == null)
                return NotFound($"Promotion with ID {id} not found.");
            existingPromotion.IsActive = !existingPromotion.IsActive;
            await _db.SaveChangesAsync();
            return Ok(new
            {
                Message = $"Promotion {(existingPromotion.IsActive ? "activated" : "deactivated")} successfully.",
                PromotionID = id,
                IsActive = existingPromotion.IsActive
            });
        }
    }
}
