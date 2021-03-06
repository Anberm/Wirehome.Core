﻿using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Resources;

namespace Wirehome.Core.HTTP.Controllers
{
    [ApiController]
    public class ResourcesController : Controller
    {
        private readonly ResourceService _resourceService;

        public ResourcesController(ResourceService resourceService)
        {
            _resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
        }

        //[HttpGet]
        //[Route("api/v1/resource_groups/uids")]
        //[ApiExplorerSettings(GroupName = "v1")]
        //public IList<string> GetResourceGroupUids()
        //{
        //    return _resourceService.GetResourceGroupUids();
        //}

        //[HttpGet]
        //[Route("api/v1/resource_groups/{uid}")]
        //[ApiExplorerSettings(GroupName = "v1")]
        //public IDictionary<string, string> GetResourcesFromGroup(string uid)
        //{
        //    return _resourceService.GetResourceByGroup(uid);
        //}

        [HttpGet]
        [Route("api/v1/resources/uids")]
        [ApiExplorerSettings(GroupName = "v1")]
        public IList<string> GetResourceUids()
        {
            return _resourceService.GetResourceUids();
        }

        [HttpGet]
        [Route("api/v1/resources/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public IDictionary<string, string> GetResource(string uid)
        {
            return _resourceService.GetResource(uid);
        }

        [HttpDelete]
        [Route("api/v1/resources/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteResource(string uid)
        {
            _resourceService.DeleteResource(uid);
        }

        [HttpGet]
        [Route("api/v1/resources/{resourceUid}/languages/{languageCode}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public string GetResourceValue(string resourceUid, string languageCode)
        {
            var value = _resourceService.GetLanguageResourceValue(resourceUid, languageCode, null);
            if (value == null)
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return value;
        }

        [HttpPost]
        [Route("api/v1/resources/{resourceUid}/languages/{languageCode}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostResourceValue(string resourceUid, string languageCode, [FromBody] string value)
        {
            _resourceService.SetResourceValue(resourceUid, languageCode, value);
        }
    }
}
