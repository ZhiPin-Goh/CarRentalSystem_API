using CarRentalSystem_API.DTO.PromotionDTO;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers.AuthControllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Tags("Auth Promotion Management")]
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
        [HttpGet]
        public async Task<IActionResult> GetActivePromtions()
        {
            var activePromotions = await _db.Promotions
                .Where(p => p.IsActive && p.StartDate <= DateTime.Now && p.EndDate >= DateTime.Now)
                .ToListAsync();

            return Ok(activePromotions);
        }
        [HttpPost]
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
            if(createPromotion.DiscountPercentage <= 0 || createPromotion.DiscountPercentage > 100)
                return  BadRequest(new
                {
                    error = "Invalid Discount Percentage",
                    message = "Discount percentage must be between 0 and 100."
                });
            if(createPromotion.MaxDiscountAmount != null)
                if (createPromotion.MaxDiscountAmount <= 0)
                    return BadRequest(new
                    {
                        error = "Invalid Max Discount Amount",
                        message = "Max discount amount must be greater than 0."
                    });

            string promtionScopeValues = createPromotion.PromotionScope;
            switch (createPromotion.PromotionScope)
            {
                case "Global": // No additional validation needed for global scope
                    createPromotion.TargetValue = null;
                    createPromotion.ApplicableModel = null;
                    break;
                case "ModelSpecific": // Vehicle model specific promotion
                    if (string.IsNullOrWhiteSpace(createPromotion.ApplicableModel))
                        return BadRequest(new { error = "Missing Model", message = "Applicable model must be specified." });
                    var existingModel = await _db.Vehicles.AnyAsync(m => m.Model.ToLower() == createPromotion.ApplicableModel.ToLower());
                    if (!existingModel)
                        return BadRequest(new
                        {
                            error = "Invalid Model",
                            message = $"No vehicle model found matching '{createPromotion.ApplicableModel}'."
                        });
                    createPromotion.TargetValue = null;
                    break;
                case "MinSpend": // Minimum spend promotion
                    if (createPromotion.TargetValue == null || createPromotion.TargetValue <= 0)
                        return BadRequest(new
                        {
                            error = "Invalid Target Value",
                            message = "Target value must be specified and greater than 0 for MinSpend promotion scope."
                        });
                    createPromotion.ApplicableModel = null;
                    break;
                default:
                    promtionScopeValues = "Global";
                    createPromotion.TargetValue = null;
                    createPromotion.ApplicableModel = null;
                    break;
            }
            var promotion = new Promotion
            {
                Name = createPromotion.Name,
                PromotionCode = createPromotion.PromotionCode,
                DiscountPercentage = createPromotion.DiscountPercentage, // 0.15m for 15% discount < This is simple
                MaxDiscountAmount = createPromotion.MaxDiscountAmount,
                StartDate = createPromotion.StartDate,
                EndDate = createPromotion.EndDate,
                IsActive = true,
                PromotionScope = promtionScopeValues,
                TargetValue = createPromotion.TargetValue,
                ApplicableModel = createPromotion.ApplicableModel
            };
            await _db.Promotions.AddAsync(promotion);
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "Promotion created successfully.",
                PromotionID = promotion.PromotionID
            });
        }
        [HttpPost]
        public async Task<IActionResult> UpdatePromotion([FromBody] UpdatePromotionDTO updatePromotion)
        {
            var existingPromotion = await _db.Promotions.FirstOrDefaultAsync(p => p.PromotionID == updatePromotion.PromotionID);
            if (existingPromotion == null)
                return NotFound(new
                {
                    error = "Promotion Not Found",
                    message = $"Promotion with ID {updatePromotion.PromotionID} not found."
                });
            if (updatePromotion.EndDate <= DateTime.UtcNow || updatePromotion.EndDate <= existingPromotion.StartDate)
                return BadRequest(new
                {
                    error = "Invalid End Date",
                    message = "End date must be in the future and after the start date."
                });

            _db.Entry(existingPromotion).CurrentValues.SetValues(updatePromotion);
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "Promotion updated successfully.",
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
                message = "Promotion deleted successfully.",
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
                message = $"Promotion {(existingPromotion.IsActive ? "activated" : "deactivated")} successfully.",
                PromotionID = id,
                IsActive = existingPromotion.IsActive
            });
        }
    }
}
