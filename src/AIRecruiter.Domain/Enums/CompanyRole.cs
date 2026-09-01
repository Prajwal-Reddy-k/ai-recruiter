namespace AIRecruiter.Domain.Enums;

/// <summary>Least-privilege role for a recruiter within their company's hiring team.
/// Every existing/seeded recruiter defaults to Owner (they already had full access to their
/// one-recruiter company before this concept existed) — see the DB default on
/// RecruiterProfile.CompanyRole.</summary>
public enum CompanyRole
{
    Owner = 1,
    Recruiter = 2,
    HiringManager = 3,
    Interviewer = 4
}
