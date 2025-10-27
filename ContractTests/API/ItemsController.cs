using ItemsApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemsController : ControllerBase
    {
        // In-memory data store with sample data
        private static readonly List<Item> Items = new()
        {
            new Item { Id = 1, Name = "Sample Item 1" },
            new Item { Id = 5, Name = "Sample Item 5" }
        };

        // GET: api/items
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Item>), StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Item>> Get()
        {
            return Ok(Items);
        }

        // GET: api/items/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Item), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<Item> GetById(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { error = "Invalid item ID." });
            }

            var item = Items.FirstOrDefault(i => i.Id == id);
            if (item == null)
            {
                return NotFound(new { error = $"Item with ID {id} not found." });
            }

            return Ok(item);
        }

        // POST: api/items
        [HttpPost]
        [ProducesResponseType(typeof(Item), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public ActionResult<Item> Post([FromBody] Item item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Name))
            {
                return BadRequest(new { error = "Item name cannot be empty." });
            }

            // Check for duplicate names
            if (Items.Any(i => i.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return Conflict(new { error = "Item with this name already exists." });
            }

            item.Id = Items.Any() ? Items.Max(i => i.Id) + 1 : 1;
            Items.Add(item);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }
    }
}
