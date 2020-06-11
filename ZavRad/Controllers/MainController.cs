using Microsoft.AspNetCore.Mvc;
using System;
using ZavRad.Models;
using ZavRad.Services;

namespace ZavRad.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MainController : ControllerBase
    {
        private readonly FileService _service;
        private readonly DataService _dataService;

        public MainController(FileService service, DataService dataService)
        {
            _service = service;
            _dataService = dataService;
        }

        [HttpPost("init")]
        public IActionResult Initialize([FromBody] InitRequest request)
        {
            var references = _dataService.Init(request.AlignmentFile, request.ReferenceFile);
            return Ok(references);
        }

        [HttpGet("sortFiles")]
        public IActionResult SortFiles()
        {
            _dataService.SortFiles();
            return Ok();
        }

        [HttpPost("calculateHist")]
        public IActionResult CalculateHistogram([FromBody] GenericRequest request)
        {
            _dataService.CalculateHist(request.ReferenceName);
            return Ok();
        }

        [HttpPost("histogram")]
        public IActionResult GetHistogram([FromBody] GenericRequest request)
        {
            var image = _service.DrawHistogram(request.ReferenceName, request.Zoom, request.Chunk);
            return Ok(image.ToArray());
        }

        [HttpPost("image")]
        public IActionResult CreateImage([FromBody] GenericRequest request)
        {
            var image = _service.GetImage(request.ReferenceName, request.Zoom, request.Chunk);
            return Ok(image);
        }

        [HttpPost("chunks")]
        public IActionResult GetChunks([FromBody] GenericRequest request)
        {
            var chunks = _service.GetChunks(request.ReferenceName, request.Zoom);
            return Ok(chunks);
        }

        [HttpPost("scaledHistogram")]
        public IActionResult GetScaledHistogram([FromBody] GenericRequest request)
        {
            var histogram = _service.GetScaledHistogram(request.ReferenceName, request.ScreenSize);
            return Ok(histogram);
        }

        [HttpPost("tooltips")]
        public IActionResult GetTooltips([FromBody] GenericRequest request)
        {
            var tooltips = _service.GetTooltips(request.ReferenceName, request.Zoom);
            return Ok(tooltips);
        }
    }
}