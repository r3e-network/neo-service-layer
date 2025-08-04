#!/bin/bash

# Disable public access and revert to localhost-only binding

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║         Reverting to Localhost-Only Access                   ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Check for backup files
backup_found=false
for file in docker-compose.phase*.yml.localhost-backup; do
    if [ -f "$file" ]; then
        backup_found=true
        break
    fi
done

if [ "$backup_found" = true ]; then
    echo -e "${BLUE}Restoring from backup files...${NC}"
    for backup in docker-compose.phase*.yml.localhost-backup; do
        if [ -f "$backup" ]; then
            original="${backup%.localhost-backup}"
            cp "$backup" "$original"
            echo -e "${GREEN}✓ Restored $original${NC}"
        fi
    done
else
    echo -e "${BLUE}No backup files found. Manually reverting changes...${NC}"
    
    # Remove 0.0.0.0: prefix from all port mappings
    for file in docker-compose.phase*.yml; do
        if [ -f "$file" ]; then
            sed -i 's/"0\.0\.0\.0:\([0-9]*\):\([0-9]*\)"/"\1:\2"/g' "$file"
            echo -e "${GREEN}✓ Updated $file${NC}"
        fi
    done
fi

# Remove public access info file
if [ -f "public-access-info.txt" ]; then
    rm public-access-info.txt
    echo -e "${GREEN}✓ Removed public access info file${NC}"
fi

echo ""
echo -e "${YELLOW}Services need to be restarted for changes to take effect.${NC}"
read -p "Restart all services now? (y/n): " restart

if [ "$restart" == "y" ] || [ "$restart" == "Y" ]; then
    echo -e "${BLUE}Restarting services...${NC}"
    ./scripts/stop-all-services.sh
    ./scripts/deploy-automatic.sh
fi

echo ""
echo -e "${GREEN}✓ Services are now accessible on localhost only${NC}"
echo -e "${GREEN}✓ Public access has been disabled${NC}"