﻿using FluentValidation;
using Planar.API.Common.Entities;
using System;

namespace Planar.Service.Validation
{
    public class GetHistoryRequestValidator : AbstractValidator<GetHistoryRequest>
    {
        public GetHistoryRequestValidator()
        {
            RuleFor(r => r.FromDate).LessThan(DateTime.Now);
            RuleFor(r => r.JobId).Null()
                .When((req, r) => !string.IsNullOrEmpty(req.JobGroup))
                .WithMessage("{PropertyName} must be null when 'Group' property is provided");

            RuleFor(r => r.JobGroup).Null()
                .When((req, r) => !string.IsNullOrEmpty(req.JobId))
                .WithMessage("{PropertyName} must be null when 'JobId' property is provided");
        }
    }
}