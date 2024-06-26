﻿using static BiddingService.Enum;

namespace BiddingService;

public class BidDto
{
    public string? Id { get; set; }
    public string? AuctionId { get; set; }
    public DateTime BidTime { get; set; } = DateTime.UtcNow;
    public int Amount { get; set; }
    public string? Bidder { get; set; }
    public string? BidStatus { get; set; }
}
