﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Wirehome.Cloud.Controllers
{
    [Authorize]
    public class ChannelController : Controller
    {
        [HttpGet]
        public IActionResult DeviceNotConnected()
        {
            return View(nameof(DeviceNotConnected));
        }

        [HttpGet]
        public IActionResult Index()
        {
            HttpContext.Response.Cookies.Append(CookieNames.ChannelUid, "default");

            // This controller allows setting the used channel. 
            // The list of channels is provided as radio buttons.
            // Direct call will open the window always.
            // Other controllers can redirect to this view if required.
            return View(nameof(Index));
        }
    }
}
