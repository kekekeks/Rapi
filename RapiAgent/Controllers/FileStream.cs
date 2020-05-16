using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RapiAgent.Utils;
using FileIO=System.IO.File;

namespace RapiAgent.Controllers
{
    public class FileStreamController : ControllerBase
    {
        [HttpGet]
        [Route("filestream/read")]
        public IActionResult ReadFile([FromQuery] string path) => 
            PhysicalFile(path, "application/octet-stream");

        [HttpPost]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue, ValueLengthLimit = int.MaxValue)]
        [Route("filestream/write")]
        public async Task<IActionResult> WriteLarge([FromQuery] string path)
        {
            using (var target = FileIO.Create(path))
                await Request.Body.CopyToAsync(target);
            return Ok();
        }
    }
}