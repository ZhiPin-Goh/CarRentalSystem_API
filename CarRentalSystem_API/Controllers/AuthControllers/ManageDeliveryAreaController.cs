using CarRentalSystem_API.DTO.DeliveryAreaDTO;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers.AuthControllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Tags("Auth Delivery Area Management")]
    public class ManageDeliveryAreaController : Controller
    {
        private readonly AppDbContext _db;
        public ManageDeliveryAreaController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllDeliveryArea()
        {
            var areas = await _db.DeliveryAreas
                .Select(a => new
                {
                    a.AreaID,
                    a.AreaName,
                    a.Fee,
                    Status = a.IsActive ? "Active" : "Inactive"
                })
                .ToListAsync();
            if (areas == null || areas.Count == 0)
            {
                return NotFound(new
                {
                    error = "No delivery areas found.",
                    message = "No active delivery areas are found in the system."
                });
            }
            return Ok(areas);
        }
        [HttpGet]
        public async Task<IActionResult> GetActiveArea()
        {
            var activeAreas = await _db.DeliveryAreas
                .Where(a => a.IsActive)
                .Select(a => new
                {
                    a.AreaID,
                    a.AreaName,
                    a.Fee
                })
                .ToListAsync();
            if (activeAreas == null || activeAreas.Count == 0)
            {
                return NotFound(new
                {
                    error = "No active delivery areas found.",
                    message = "No active delivery areas are found in the system."
                });
            }
            return Ok(activeAreas);
        }
        [HttpPost]
        public async Task<IActionResult> CreateNewArea([FromBody] CreateAreaDTO createArea)
        {
            if (createArea.Fee < 0)
            {
                return BadRequest(new
                {
                    error = "Invalid fee value.",
                    message = "The fee must be a non-negative value."
                });
            }
            var existingArea = await _db.DeliveryAreas
                .FirstOrDefaultAsync(a => a.AreaName.ToLower() == createArea.AreaName.ToLower());
            if (existingArea != null)
            {
                return BadRequest(new
                {
                    error = "Area already exists.",
                    message = $"A delivery area with the name '{createArea.AreaName}' already exists."
                });
            }
            var newArea = new DeliveryArea
            {
                AreaName = createArea.AreaName,
                Fee = createArea.Fee,
                IsActive = true
            };
            await _db.DeliveryAreas.AddAsync(newArea);
            await _db.SaveChangesAsync();
            return Ok(new {
                message = "New delivery area created successfully.",
                AreaID = newArea.AreaID,
                AreaName = newArea.AreaName,
                Fee = newArea.Fee
            });
        }
        [HttpPost]
        public async Task<IActionResult> UpdateArea([FromBody] UpdateAreaDTO updateArea)
        {
            var area = await _db.DeliveryAreas.FindAsync(updateArea.AreaID);
            if (area == null)
                return NotFound(new {
                    error = "Area not found.",
                    message = $"No delivery area found with ID {updateArea.AreaID}."
                });
            if (updateArea.Fee < 0)
            {
                return BadRequest(new
                {
                    error = "Invalid fee value.",
                    message = "The fee must be a non-negative value."
                });
            }
             _db.Entry(area).CurrentValues.SetValues(updateArea);
            await _db.SaveChangesAsync();
            return Ok(new {
                message = "Delivery area updated successfully.",
                AreaID = area.AreaID,
                AreaName = area.AreaName,
                Fee = area.Fee
            });
        }
        [HttpPost ("{areaID}")]
        public async Task<IActionResult> ToggleAreaStatus(int areaID)
        {
            var area = await _db.DeliveryAreas.FirstOrDefaultAsync(a => a.AreaID == areaID);
            if(area == null) 
                return NotFound(new {
                    error = "Area not found.",
                    message = $"No delivery area found with ID {areaID}."
                });
            area.IsActive = !area.IsActive;
            await _db.SaveChangesAsync();
            return Ok(new {
                message = $"Delivery area {(area.IsActive ? "activated" : "deactivated")} successfully.",
                AreaID = area.AreaID,
                AreaName = area.AreaName,
                Fee = area.Fee,
                IsActive = area.IsActive
            });
        }
    }
}
