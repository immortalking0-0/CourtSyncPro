using CourtSyncPro.Models.Entities;

namespace CourtSyncPro.Services
{
    // ── This class holds a full breakdown of how the price was calculated ──
    public class PriceBreakdown
    {
        public decimal BasePrice { get; set; }
        public decimal FinalPrice { get; set; }
        public decimal TotalChange { get; set; }  // + or - from base
        public string SeasonLabel { get; set; } = "";
        public string TimeLabel { get; set; } = "";
        public string RatingLabel { get; set; } = "";
        public string DayLabel { get; set; } = "";
        public decimal SeasonMultiplier { get; set; }
        public decimal TimeMultiplier { get; set; }
        public decimal RatingMultiplier { get; set; }
        public decimal DayMultiplier { get; set; }
        public string PriceTag { get; set; } = ""; // "Peak Price", "Discount", etc
        public string TagColor { get; set; } = ""; // green, red, orange
    }

    public class DynamicPricingService
    {
        // ════════════════════════════════════════════════════════════
        // MAIN METHOD — Call this to get the final price + breakdown
        // ════════════════════════════════════════════════════════════
        public PriceBreakdown Calculate(
            decimal basePrice,
            DateTime slotStart,
            float courtRating,
            int bookingsToday = 0)
        {
            decimal total = 1.0m; // Start at 100% (no change)

            // ── Factor 1: Season ──────────────────────────────────
            var (seasonMult, seasonLabel) = GetSeasonMultiplier(slotStart);
            total += (seasonMult - 1.0m); // Add change to total

            // ── Factor 2: Time of day ─────────────────────────────
            var (timeMult, timeLabel) = GetTimeMultiplier(slotStart);
            total += (timeMult - 1.0m);

            // ── Factor 3: Day of week ─────────────────────────────
            var (dayMult, dayLabel) = GetDayMultiplier(slotStart);
            total += (dayMult - 1.0m);

            // ── Factor 4: Court rating ────────────────────────────
            var (ratingMult, ratingLabel) = GetRatingMultiplier(courtRating);
            total += (ratingMult - 1.0m);

            // ── Factor 5: Live demand ─────────────────────────────
            if (bookingsToday > 20) total += 0.20m;
            else if (bookingsToday > 12) total += 0.10m;

            // ── Safety cap: never below 60% or above 250% ─────────
            total = Math.Max(0.60m, Math.Min(2.50m, total));

            decimal finalPrice = Math.Round(basePrice * total, 0);

            // ── Build the price tag ───────────────────────────────
            string tag, color;
            if (total > 1.20m) { tag = "🔥 Peak Price"; color = "danger"; }
            else if (total > 1.0m) { tag = "📈 Higher Price"; color = "warning"; }
            else if (total < 0.85m) { tag = "🏷 Special Deal"; color = "success"; }
            else if (total < 1.0m) { tag = "💚 Off-Peak Deal"; color = "success"; }
            else { tag = "Standard Price"; color = "secondary"; }

            return new PriceBreakdown
            {
                BasePrice = basePrice,
                FinalPrice = finalPrice,
                TotalChange = finalPrice - basePrice,
                SeasonLabel = seasonLabel,
                TimeLabel = timeLabel,
                RatingLabel = ratingLabel,
                DayLabel = dayLabel,
                SeasonMultiplier = seasonMult,
                TimeMultiplier = timeMult,
                RatingMultiplier = ratingMult,
                DayMultiplier = dayMult,
                PriceTag = tag,
                TagColor = color,
            };
        }

        // ════════════════════════════════════════════════════════════
        // FACTOR 1: SEASON
        // Pakistan seasons:
        //   Summer  = May, Jun, Jul, Aug       → +20% (hot = more indoor)
        //   Monsoon = Jul, Aug, Sep             → +15% (rainy = prefer indoor)
        //   Winter  = Dec, Jan, Feb             → -10% (cold = people stay home)
        //   Spring  = Mar, Apr                  → +5%  (pleasant, moderate demand)
        //   Autumn  = Oct, Nov                  → standard
        // ════════════════════════════════════════════════════════════
        private (decimal multiplier, string label) GetSeasonMultiplier(DateTime date)
        {
            int month = date.Month;

            // Monsoon check first (overlaps with summer)
            if (month == 7 || month == 8)
                return (1.30m, "☔ Monsoon Season (+30%) — Rain drives players indoors!");

            if (month is 5 or 6)
                return (1.20m, "☀ Summer Season (+20%) — Hot outside, cool indoors!");

            if (month == 9)
                return (1.10m, "🌧 Post-Monsoon (+10%) — Still rainy, indoor preferred.");

            if (month is 12 or 1 or 2)
                return (0.90m, "❄ Winter Season (-10%) — Slower demand in cold months.");

            if (month is 3 or 4)
                return (1.05m, "🌸 Spring Season (+5%) — Pleasant weather, good demand.");

            // October, November — standard
            return (1.0m, "🍂 Autumn Season — Standard pricing.");
        }

        // ════════════════════════════════════════════════════════════
        // FACTOR 2: TIME OF DAY
        //   Late night    10 PM - 12 AM → -25%  (very slow)
        //   Early morning 12 AM - 6 AM  → -30%  (almost no bookings)
        //   Morning peak  6 AM - 9 AM   → +10%  (before work/school)
        //   Off-peak      9 AM - 12 PM  → -15%  (slow hours)
        //   Lunch peak    12 PM - 2 PM  → +10%  (lunch break games)
        //   Off-peak      2 PM - 5 PM   → -5%   (quietest time)
        //   Evening peak  5 PM - 8 PM   → +35%  (most popular hours)
        //   Night peak    8 PM - 10 PM  → +20%  (still busy)
        // ════════════════════════════════════════════════════════════
        private (decimal multiplier, string label) GetTimeMultiplier(DateTime date)
        {
            int hour = date.Hour;

            return hour switch
            {
                >= 0 and < 6 =>
                    (0.70m, "🌙 Early Morning (12–6 AM) — Very slow. -30% discount."),

                >= 6 and < 9 =>
                    (1.10m, "🌅 Morning Peak (6–9 AM) — Pre-work/school. +10%."),

                >= 9 and < 12 =>
                    (0.85m, "🕙 Off-Peak (9 AM–12 PM) — Quiet hours. -15% discount."),

                >= 12 and < 14 =>
                    (1.10m, "🍽 Lunch Peak (12–2 PM) — Lunch break players. +10%."),

                >= 14 and < 17 =>
                    (0.95m, "🕒 Afternoon (2–5 PM) — Slowest time. -5% discount."),

                >= 17 and < 20 =>
                    (1.35m, "🔥 Evening Peak (5–8 PM) — Most popular! +35% surge."),

                >= 20 and < 22 =>
                    (1.20m, "🌆 Night Peak (8–10 PM) — Still busy. +20%."),

                _ =>
                    (0.75m, "🌃 Late Night (10 PM–12 AM) — Very slow. -25% discount.")
            };
        }

        // ════════════════════════════════════════════════════════════
        // FACTOR 3: DAY OF WEEK
        // Pakistan weekend = Friday & Saturday
        //   Friday     → +30%  (biggest game day in Pakistan)
        //   Saturday   → +25%  (holiday, full day free)
        //   Sunday     → +10%  (some offices closed)
        //   Wednesday  → -10%  (mid-week slump)
        //   Mon/Tue/Thu → standard
        // ════════════════════════════════════════════════════════════
        private (decimal multiplier, string label) GetDayMultiplier(DateTime date)
        {
            return date.DayOfWeek switch
            {
                DayOfWeek.Friday =>
                    (1.30m, "🕌 Friday (+30%) — Biggest sports day in Pakistan!"),

                DayOfWeek.Saturday =>
                    (1.25m, "🎉 Saturday (+25%) — Full holiday, high demand."),

                DayOfWeek.Sunday =>
                    (1.10m, "🏃 Sunday (+10%) — Many offices closed."),

                DayOfWeek.Wednesday =>
                    (0.90m, "📅 Wednesday (-10%) — Mid-week discount."),

                _ => (1.0m, "📅 Weekday — Standard pricing.")
            };
        }

        // ════════════════════════════════════════════════════════════
        // FACTOR 4: COURT RATING
        // Higher rated courts deserve higher prices
        // Lower rated courts need discounts to attract bookings
        //   4.6 – 5.0 → +25%  (premium courts)
        //   4.1 – 4.5 → +15%  (very good courts)
        //   3.6 – 4.0 → +5%   (good courts)
        //   3.0 – 3.5 → 0%    (average)
        //   2.0 – 2.9 → -15%  (below average)
        //   0.0 – 1.9 → -25%  (new/poor courts)
        // ════════════════════════════════════════════════════════════
        private (decimal multiplier, string label) GetRatingMultiplier(float rating)
        {
            return rating switch
            {
                >= 4.6f =>
                    (1.25m, $"⭐ Premium Court ({rating:0.0}/5) — +25% for top quality."),

                >= 4.1f =>
                    (1.15m, $"⭐ Excellent Court ({rating:0.0}/5) — +15%."),

                >= 3.6f =>
                    (1.05m, $"⭐ Good Court ({rating:0.0}/5) — +5%."),

                >= 3.0f =>
                    (1.0m, $"⭐ Average Court ({rating:0.0}/5) — Standard price."),

                >= 2.0f =>
                    (0.85m, $"⭐ Below Average ({rating:0.0}/5) — -15% to attract bookings."),

                _ =>
                    (0.75m, $"⭐ New/Low Rated ({rating:0.0}/5) — -25% introductory price.")
            };
        }

        // ════════════════════════════════════════════════════════════
        // HELPER: Get price for a specific slot quickly (no breakdown)
        // ════════════════════════════════════════════════════════════
        public decimal GetFinalPrice(decimal basePrice, DateTime slotStart,
                                     float courtRating, int bookingsToday = 0)
        {
            return Calculate(basePrice, slotStart, courtRating, bookingsToday).FinalPrice;
        }

        // ════════════════════════════════════════════════════════════
        // HELPER: Get a summary label for a slot (for listing pages)
        // ════════════════════════════════════════════════════════════
        public string GetPriceSummary(decimal basePrice, decimal finalPrice)
        {
            decimal diff = finalPrice - basePrice;
            decimal pct = Math.Abs(diff / basePrice * 100);

            if (diff > 0) return $"+{pct:0}% Peak";
            if (diff < 0) return $"-{pct:0}% Off";
            return "Standard";
        }
    }
}