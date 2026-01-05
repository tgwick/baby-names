#!/bin/bash
set -e

# ============================================
# NameMatch Infrastructure Deployment Script
# ============================================

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default values
ENVIRONMENT=""
ACTION="plan"
LOCATION="eastus"

# Usage
usage() {
    echo "Usage: $0 -e <environment> [-a <action>] [-l <location>]"
    echo ""
    echo "Options:"
    echo "  -e    Environment (required): dev or prod"
    echo "  -a    Action: plan (default) or deploy"
    echo "  -l    Location: Azure region (default: eastus)"
    echo ""
    echo "Examples:"
    echo "  $0 -e dev -a plan       # Preview dev deployment"
    echo "  $0 -e dev -a deploy     # Deploy to dev"
    echo "  $0 -e prod -a deploy    # Deploy to production"
    exit 1
}

# Parse arguments
while getopts "e:a:l:h" opt; do
    case $opt in
        e) ENVIRONMENT="$OPTARG" ;;
        a) ACTION="$OPTARG" ;;
        l) LOCATION="$OPTARG" ;;
        h) usage ;;
        *) usage ;;
    esac
done

# Validate environment
if [[ -z "$ENVIRONMENT" ]]; then
    echo -e "${RED}Error: Environment is required${NC}"
    usage
fi

if [[ "$ENVIRONMENT" != "dev" && "$ENVIRONMENT" != "prod" ]]; then
    echo -e "${RED}Error: Environment must be 'dev' or 'prod'${NC}"
    exit 1
fi

# Validate action
if [[ "$ACTION" != "plan" && "$ACTION" != "deploy" ]]; then
    echo -e "${RED}Error: Action must be 'plan' or 'deploy'${NC}"
    exit 1
fi

# Check for required secrets
if [[ -z "$POSTGRES_ADMIN_PASSWORD" ]]; then
    echo -e "${YELLOW}Warning: POSTGRES_ADMIN_PASSWORD not set${NC}"
    read -sp "Enter PostgreSQL admin password: " POSTGRES_ADMIN_PASSWORD
    echo ""
fi

if [[ -z "$JWT_KEY" ]]; then
    echo -e "${YELLOW}Warning: JWT_KEY not set${NC}"
    read -sp "Enter JWT signing key (min 32 chars): " JWT_KEY
    echo ""
fi

# Validate JWT key length
if [[ ${#JWT_KEY} -lt 32 ]]; then
    echo -e "${RED}Error: JWT_KEY must be at least 32 characters${NC}"
    exit 1
fi

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}NameMatch Infrastructure Deployment${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Environment: $ENVIRONMENT"
echo "Action:      $ACTION"
echo "Location:    $LOCATION"
echo ""

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BICEP_DIR="$SCRIPT_DIR/../bicep"

# Check if logged in to Azure
echo -e "${YELLOW}Checking Azure login...${NC}"
if ! az account show &> /dev/null; then
    echo -e "${RED}Not logged in to Azure. Running 'az login'...${NC}"
    az login
fi

SUBSCRIPTION=$(az account show --query name -o tsv)
echo "Subscription: $SUBSCRIPTION"
echo ""

# Deployment name
DEPLOYMENT_NAME="namematch-${ENVIRONMENT}-$(date +%Y%m%d-%H%M%S)"

if [[ "$ACTION" == "plan" ]]; then
    echo -e "${YELLOW}Running what-if deployment (preview)...${NC}"
    az deployment sub what-if \
        --name "$DEPLOYMENT_NAME" \
        --location "$LOCATION" \
        --template-file "$BICEP_DIR/main.bicep" \
        --parameters environment="$ENVIRONMENT" \
        --parameters location="$LOCATION" \
        --parameters postgresAdminPassword="$POSTGRES_ADMIN_PASSWORD" \
        --parameters jwtKey="$JWT_KEY"
else
    echo -e "${YELLOW}Deploying infrastructure...${NC}"
    az deployment sub create \
        --name "$DEPLOYMENT_NAME" \
        --location "$LOCATION" \
        --template-file "$BICEP_DIR/main.bicep" \
        --parameters environment="$ENVIRONMENT" \
        --parameters location="$LOCATION" \
        --parameters postgresAdminPassword="$POSTGRES_ADMIN_PASSWORD" \
        --parameters jwtKey="$JWT_KEY" \
        --output table

    echo ""
    echo -e "${GREEN}Deployment complete!${NC}"
    echo ""
    echo "Getting deployment outputs..."
    az deployment sub show \
        --name "$DEPLOYMENT_NAME" \
        --query properties.outputs \
        --output table
fi

echo ""
echo -e "${GREEN}Done!${NC}"
