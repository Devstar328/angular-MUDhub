﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MUDhub.Server.ApiModels.Muds
{
    public class MudCreationResponse : BaseResponse
    {
        public MudApiModel? Mud { get; set; } = null;
    }
}