﻿namespace MUDhub.Server.Options
{
    public class SpaConfiguration
    {
        public bool IntegratedHosting { get; set; } = true;
        public string RelativePath { get; set; } = "client";
        public string ExternalHostingUrl { get; set; } = "4200";
    }
}