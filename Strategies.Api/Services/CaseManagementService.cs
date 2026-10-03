using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using System;
using System.Threading.Tasks;

namespace Strategies.Api.Services
{
    public class CaseManagementService
    {
        private readonly ApplicationDbContext _context;

        public CaseManagementService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AdvanceCaseStageAsync(Guid caseId)
        {
            var caseRecord = await _context.Cases.FindAsync(caseId);
            if (caseRecord == null) return false;

            caseRecord.Stage = caseRecord.Stage switch
            {
                CaseStage.InitialIntake => CaseStage.DocumentGathering,
                CaseStage.DocumentGathering => CaseStage.UnderReview,
                CaseStage.UnderReview => CaseStage.SubmittedToAuthority,
                CaseStage.SubmittedToAuthority => CaseStage.FinalDecision,
                _ => caseRecord.Stage
            };

            caseRecord.UpdatedAt = DateTime.UtcNow; // Fixed: Matches your DB model
            await _context.SaveChangesAsync();
            return true;
        }
    }
}