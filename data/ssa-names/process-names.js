/**
 * Process SSA baby names CSV into a JSON file for database seeding.
 *
 * Output format:
 * - Aggregates by name
 * - Determines primary gender (gender with higher total percentage)
 * - Calculates popularity score (1-100 based on total usage)
 * - Filters to recent years (1970+) for more relevant names
 */

const fs = require('fs');
const path = require('path');

const inputFile = path.join(__dirname, 'names.csv');
const outputFile = path.join(__dirname, 'processed-names.json');

// Read and parse CSV
const csv = fs.readFileSync(inputFile, 'utf8');
const lines = csv.split('\n').slice(1); // Skip header

// Aggregate data by name
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

    // Only include names from 1970 onwards for relevance
    if (year < 1970) continue;

    const normalizedName = name.charAt(0).toUpperCase() + name.slice(1).toLowerCase();

    if (!nameStats.has(normalizedName)) {
        nameStats.set(normalizedName, {
            name: normalizedName,
            malePercent: 0,
            femalePercent: 0,
            totalUsage: 0
        });
    }

    const stats = nameStats.get(normalizedName);
    if (gender === 'male') {
        stats.malePercent += percent;
    } else {
        stats.femalePercent += percent;
    }
    stats.totalUsage += percent;
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

    // Calculate popularity score (1-100)
    const popularityScore = Math.max(1, getPercentile(stats.totalUsage));

    names.push({
        nameText: stats.name,
        gender,
        popularityScore,
        origin: null // Could be enhanced with origin data later
    });
}

// Sort by popularity (highest first) and take top 10,000
names.sort((a, b) => b.popularityScore - a.popularityScore);
const topNames = names.slice(0, 10000);

console.log(`Processed ${nameStats.size} unique names`);
console.log(`Selected top ${topNames.length} names`);
console.log(`Gender distribution:`);
console.log(`  Male: ${topNames.filter(n => n.gender === 0).length}`);
console.log(`  Female: ${topNames.filter(n => n.gender === 1).length}`);
console.log(`  Neutral: ${topNames.filter(n => n.gender === 2).length}`);

// Write output
fs.writeFileSync(outputFile, JSON.stringify(topNames, null, 2));
console.log(`\nOutput written to ${outputFile}`);
