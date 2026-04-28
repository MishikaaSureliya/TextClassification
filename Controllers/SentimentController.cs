using Microsoft.AspNetCore.Mvc;
using TextClassification.Services;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace TextClassification.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SentimentController : ControllerBase
    {
        private readonly MLService _mlService;

        public SentimentController(MLService mlService)
        {
            _mlService = mlService;
        }
        [Authorize]
        // ✅ POST: api/sentiment/predict
        [HttpPost("predict")]
        public IActionResult Predict([FromBody] string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return BadRequest("Text cannot be empty");
            }

            var result = _mlService.Predict(text);

            // 👉 Get highest probability from Score array
            var probability = result.Score != null ? result.Score.Max() : 0;

            return Ok(new
            {
                Text = text,
                Prediction = result.Prediction,
                Probability = probability
            });
        }
    }
}