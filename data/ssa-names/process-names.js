/**
 * Process SSA baby names CSV into a JSON file for database seeding.
 *
 * Output format:
 * - Aggregates by name with year-by-year tracking
 * - Determines primary gender (gender with higher total percentage)
 * - Calculates popularity score (1-100 based on total usage)
 * - Calculates trend score (-1 to +1: declining to rising)
 * - Calculates stability score (0 to 1: volatile to consistent)
 * - Determines peak decade
 * - Filters to recent years (1970+) for more relevant names
 */

const fs = require('fs');
const path = require('path');

const inputFile = path.join(__dirname, 'names.csv');
const outputFile = path.join(__dirname, 'processed-names.json');

// Configuration
// Note: SSA data goes from 1880 to 2008
const MIN_YEAR = 1950; // Include more history for classic detection
const RECENT_YEARS_START = 1995; // "Modern era" - last ~15 years of data
const HISTORICAL_YEARS_END = 1980; // "Classic era" - pre-modern names

// Read and parse CSV
const csv = fs.readFileSync(inputFile, 'utf8');
const lines = csv.split('\n').slice(1); // Skip header

// Aggregate data by name with year tracking
const nameStats = new Map();

for (const line of lines) {
    if (!line.trim()) continue;

    // Parse CSV line: "year","name","percent","sex"
    const match = line.match(/(\d+),"([^"]+)",([0-9.]+),"(boy|girl)"/);
    if (!match) continue;

    const [, yearStr, name, percentStr, sex] = match;
    const year = parseInt(yearStr);
    const percent = parseFloat(percentStr);
    const gender = sex === 'boy' ? 'male' : 'female';

    // Only include names from MIN_YEAR onwards for relevance
    if (year < MIN_YEAR) continue;

    const normalizedName = name.charAt(0).toUpperCase() + name.slice(1).toLowerCase();

    if (!nameStats.has(normalizedName)) {
        nameStats.set(normalizedName, {
            name: normalizedName,
            malePercent: 0,
            femalePercent: 0,
            totalUsage: 0,
            yearlyData: new Map(), // year -> { male: percent, female: percent }
        });
    }

    const stats = nameStats.get(normalizedName);

    // Track overall totals
    if (gender === 'male') {
        stats.malePercent += percent;
    } else {
        stats.femalePercent += percent;
    }
    stats.totalUsage += percent;

    // Track yearly data
    if (!stats.yearlyData.has(year)) {
        stats.yearlyData.set(year, { male: 0, female: 0 });
    }
    stats.yearlyData.get(year)[gender === 'male' ? 'male' : 'female'] += percent;
}

/**
 * Calculate trend score: how much has popularity changed recently vs historically?
 * Returns -1 (declining) to +1 (rising), 0 = stable
 */
function calculateTrendScore(yearlyData) {
    let recentSum = 0, recentCount = 0;
    let historicalSum = 0, historicalCount = 0;

    for (const [year, data] of yearlyData) {
        const total = data.male + data.female;

        if (year >= RECENT_YEARS_START) {
            recentSum += total;
            recentCount++;
        } else if (year <= HISTORICAL_YEARS_END) {
            historicalSum += total;
            historicalCount++;
        }
    }

    if (recentCount === 0 || historicalCount === 0) {
        // Not enough data - if only recent data, it's likely trendy
        if (recentCount > 0 && historicalCount === 0) return 0.8;
        // If only historical data, it's likely fading
        if (historicalCount > 0 && recentCount === 0) return -0.8;
        return 0;
    }

    const recentAvg = recentSum / recentCount;
    const historicalAvg = historicalSum / historicalCount;

    if (historicalAvg === 0) {
        return recentAvg > 0 ? 1 : 0;
    }

    // Calculate relative change, clamped to -1 to +1
    const relativeChange = (recentAvg - historicalAvg) / Math.max(recentAvg, historicalAvg);
    return Math.max(-1, Math.min(1, relativeChange));
}

/**
 * Calculate stability score: how consistent is the name's popularity over time?
 * Returns 0 (volatile) to 1 (very stable)
 */
function calculateStabilityScore(yearlyData, totalUsage) {
    if (yearlyData.size < 5) return 0.5; // Not enough data

    const yearlyTotals = [];
    for (const [, data] of yearlyData) {
        yearlyTotals.push(data.male + data.female);
    }

    const mean = yearlyTotals.reduce((a, b) => a + b, 0) / yearlyTotals.length;
    if (mean === 0) return 0.5;

    // Calculate coefficient of variation (std dev / mean)
    const variance = yearlyTotals.reduce((sum, val) => sum + Math.pow(val - mean, 2), 0) / yearlyTotals.length;
    const stdDev = Math.sqrt(variance);
    const cv = stdDev / mean;

    // Convert CV to stability score (lower CV = higher stability)
    // CV of 0 = perfect stability (1.0), CV of 2+ = very volatile (0.0)
    const stability = Math.max(0, Math.min(1, 1 - (cv / 2)));
    return Math.round(stability * 100) / 100;
}

/**
 * Find the decade when the name was most popular
 */
function findPeakDecade(yearlyData) {
    const decadeTotals = new Map();

    for (const [year, data] of yearlyData) {
        const decade = Math.floor(year / 10) * 10;
        const total = data.male + data.female;
        decadeTotals.set(decade, (decadeTotals.get(decade) || 0) + total);
    }

    let peakDecade = 2000;
    let peakValue = 0;

    for (const [decade, total] of decadeTotals) {
        if (total > peakValue) {
            peakValue = total;
            peakDecade = decade;
        }
    }

    return peakDecade;
}

/**
 * Count how many decades the name appears in
 */
function countDecadesPresent(yearlyData) {
    const decades = new Set();
    for (const [year] of yearlyData) {
        decades.add(Math.floor(year / 10) * 10);
    }
    return decades.size;
}

// Convert to array and calculate final values
const names = [];
const allUsages = [];

for (const stats of nameStats.values()) {
    allUsages.push(stats.totalUsage);
}

// Calculate percentile thresholds for popularity scoring
allUsages.sort((a, b) => a - b);
const getPercentile = (value) => {
    const idx = allUsages.findIndex(v => v >= value);
    return Math.round((idx / allUsages.length) * 100);
};

for (const stats of nameStats.values()) {
    // Determine gender: if >70% one gender, assign that; otherwise Neutral
    const totalPercent = stats.malePercent + stats.femalePercent;
    let gender;

    if (totalPercent === 0) continue;

    const maleRatio = stats.malePercent / totalPercent;
    if (maleRatio >= 0.7) {
        gender = 0; // Male
    } else if (maleRatio <= 0.3) {
        gender = 1; // Female
    } else {
        gender = 2; // Neutral (unisex)
    }

    // Calculate scores
    const popularityScore = Math.max(1, getPercentile(stats.totalUsage));
    const trendScore = calculateTrendScore(stats.yearlyData);
    const stabilityScore = calculateStabilityScore(stats.yearlyData, stats.totalUsage);
    const peakDecade = findPeakDecade(stats.yearlyData);
    const decadesPresent = countDecadesPresent(stats.yearlyData);

    names.push({
        nameText: stats.name,
        gender,
        popularityScore,
        trendScore: Math.round(trendScore * 100) / 100,
        stabilityScore,
        peakDecade,
        decadesPresent,
        origin: null
    });
}

// Sort by popularity (highest first) and take top 10,000
names.sort((a, b) => b.popularityScore - a.popularityScore);
const topNames = names.slice(0, 10000);

// Stats
console.log(`Processed ${nameStats.size} unique names`);
console.log(`Selected top ${topNames.length} names`);
console.log(`Gender distribution:`);
console.log(`  Male: ${topNames.filter(n => n.gender === 0).length}`);
console.log(`  Female: ${topNames.filter(n => n.gender === 1).length}`);
console.log(`  Neutral: ${topNames.filter(n => n.gender === 2).length}`);

// Trend distribution
const trendy = topNames.filter(n => n.trendScore > 0.3 && n.peakDecade >= 2000);
const classic = topNames.filter(n => n.stabilityScore > 0.5 && n.decadesPresent >= 4);
const rising = topNames.filter(n => n.trendScore > 0.3);
const declining = topNames.filter(n => n.trendScore < -0.3);

console.log(`\nTrend analysis:`);
console.log(`  Trendy (rising + recent peak): ${trendy.length}`);
console.log(`  Classic (stable + long presence): ${classic.length}`);
console.log(`  Rising (trend > 0.3): ${rising.length}`);
console.log(`  Declining (trend < -0.3): ${declining.length}`);

// Sample outputs
console.log(`\nSample trendy names:`);
trendy.slice(0, 10).forEach(n =>
    console.log(`  ${n.nameText}: trend=${n.trendScore}, peak=${n.peakDecade}, pop=${n.popularityScore}`)
);

console.log(`\nSample classic names:`);
classic.filter(n => n.popularityScore > 70).slice(0, 10).forEach(n =>
    console.log(`  ${n.nameText}: stability=${n.stabilityScore}, decades=${n.decadesPresent}, pop=${n.popularityScore}`)
);

// Write output
fs.writeFileSync(outputFile, JSON.stringify(topNames, null, 2));
console.log(`\nOutput written to ${outputFile}`);
