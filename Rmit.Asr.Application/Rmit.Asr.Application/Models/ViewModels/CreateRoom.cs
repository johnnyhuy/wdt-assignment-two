﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Rmit.Asr.Application.Models.ViewModels
{
    public class CreateRoom : Room
    {
        [Required]
        public override string Name { get; set; }
    }
}