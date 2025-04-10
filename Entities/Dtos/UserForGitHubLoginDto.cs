using System;
using System.Collections.Generic;
using System.Text;
using Core.Entities;

namespace Entities.Dtos
{
    public class UserForGitHubLoginDto : IDto
    {
        public string Code { get; set; }
        public string State { get; set; }
    }
}