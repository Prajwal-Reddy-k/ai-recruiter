using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Offers;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Authorize]
public class OffersController : ControllerBase
{
    private readonly IOfferService _offers;

    public OffersController(IOfferService offers)
    {
        _offers = offers;
    }

    [HttpPost("api/applications/{applicationId:int}/offers")]
    [Authorize(Roles = "Recruiter")]
    public async Task<ActionResult<OfferDto>> CreateDraft(int applicationId, CreateOfferRequest request, CancellationToken ct) =>
        Ok(await _offers.CreateDraftAsync(User.GetUserId(), applicationId, request, ct));

    [HttpGet("api/applications/{applicationId:int}/offers")]
    public async Task<ActionResult<IReadOnlyList<OfferDto>>> GetForApplication(int applicationId, CancellationToken ct) =>
        Ok(await _offers.GetForApplicationAsync(User.GetUserId(), User.GetRole(), applicationId, ct));

    [HttpPut("api/offers/{id:int}")]
    [Authorize(Roles = "Recruiter")]
    public async Task<ActionResult<OfferDto>> UpdateDraft(int id, UpdateOfferRequest request, CancellationToken ct) =>
        Ok(await _offers.UpdateDraftAsync(User.GetUserId(), id, request, ct));

    [HttpPost("api/offers/{id:int}/send")]
    [Authorize(Roles = "Recruiter")]
    public async Task<ActionResult<OfferDto>> Send(int id, CancellationToken ct) =>
        Ok(await _offers.SendAsync(User.GetUserId(), id, ct));

    [HttpPost("api/offers/{id:int}/withdraw")]
    [Authorize(Roles = "Recruiter")]
    public async Task<ActionResult<OfferDto>> Withdraw(int id, CancellationToken ct) =>
        Ok(await _offers.WithdrawAsync(User.GetUserId(), id, ct));

    [HttpGet("api/offers/{id:int}")]
    public async Task<ActionResult<OfferDetailDto>> GetDetail(int id, CancellationToken ct) =>
        Ok(await _offers.GetDetailAsync(User.GetUserId(), User.GetRole(), id, ct));

    [HttpGet("api/offers/my")]
    [Authorize(Roles = "Candidate")]
    public async Task<ActionResult<IReadOnlyList<OfferDto>>> GetMine(CancellationToken ct) =>
        Ok(await _offers.GetMyOffersAsync(User.GetUserId(), ct));

    [HttpPost("api/offers/{id:int}/respond")]
    [Authorize(Roles = "Candidate")]
    public async Task<ActionResult<OfferDto>> Respond(int id, RespondToOfferRequest request, CancellationToken ct) =>
        Ok(await _offers.RespondAsync(User.GetUserId(), id, request, ct));
}
