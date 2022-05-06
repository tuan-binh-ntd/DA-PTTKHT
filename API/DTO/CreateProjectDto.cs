﻿using API.Enum;
using System;

namespace API.DTO
{
    public class CreateProjectDto
    {
        public string ProjectName { get; set; }
        public string Description { get; set; }
        public string ProjectType { get; set; }
        public string ProjectCode { get; set; }
        public DateTime DeadlineDate { get; set; }
        public Priority PriorityCode { get; set; }
        public StatusCode StatusCode { get; set; }
        public Guid DepartmentId { get; set; }
    }
}
