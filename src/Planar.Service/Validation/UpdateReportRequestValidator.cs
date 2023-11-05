﻿using FluentValidation;
using Planar.Api.Common.Entities;
using Planar.Service.Reports;
using System;

namespace Planar.Service.Validation
{
    public class UpdateReportRequestValidator : AbstractValidator<UpdateReportRequest>
    {
        public UpdateReportRequestValidator()
        {
            RuleFor(e => e.Group).Length(2, 50);
            RuleFor(e => e.Period).NotEmpty().IsEnumName(typeof(ReportPeriods), caseSensitive: false);
        }
    }
}