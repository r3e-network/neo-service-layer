#!/usr/bin/env python3
"""
Test Result Cache Manager
Caches test results to speed up repeated test runs
"""

import json
import hashlib
import time
from pathlib import Path
from datetime import datetime, timedelta

class TestCacheManager:
    def __init__(self):
        self.project_root = Path("/home/ubuntu/neo-service-layer")
        self.cache_dir = self.project_root / ".test-cache"
        self.cache_dir.mkdir(exist_ok=True)
        self.cache_file = self.cache_dir / "test-results-cache.json"
        self.cache_ttl_hours = 24  # Cache valid for 24 hours
        self.cache = self.load_cache()
    
    def load_cache(self):
        """Load existing cache or create new one"""
        if self.cache_file.exists():
            try:
                with open(self.cache_file) as f:
                    return json.load(f)
            except:
                return {}
        return {}
    
    def save_cache(self):
        """Save cache to disk"""
        with open(self.cache_file, 'w') as f:
            json.dump(self.cache, f, indent=2, default=str)
    
    def get_file_hash(self, file_path):
        """Get hash of a file for cache invalidation"""
        if not Path(file_path).exists():
            return None
        
        with open(file_path, 'rb') as f:
            return hashlib.md5(f.read()).hexdigest()
    
    def get_cache_key(self, test_assembly):
        """Generate cache key for a test assembly"""
        # Include file hash and modification time
        dll_path = Path(test_assembly)
        if dll_path.exists():
            file_hash = self.get_file_hash(dll_path)
            mtime = dll_path.stat().st_mtime
            return f"{dll_path.name}_{file_hash}_{mtime}"
        return None
    
    def is_cache_valid(self, cache_entry):
        """Check if cache entry is still valid"""
        if not cache_entry:
            return False
        
        # Check TTL
        cached_time = datetime.fromisoformat(cache_entry.get('timestamp', ''))
        if datetime.now() - cached_time > timedelta(hours=self.cache_ttl_hours):
            return False
        
        return True
    
    def get_cached_result(self, test_assembly):
        """Get cached test result if available"""
        cache_key = self.get_cache_key(test_assembly)
        if not cache_key:
            return None
        
        cache_entry = self.cache.get(cache_key)
        if self.is_cache_valid(cache_entry):
            return cache_entry.get('result')
        
        return None
    
    def cache_result(self, test_assembly, result):
        """Cache a test result"""
        cache_key = self.get_cache_key(test_assembly)
        if cache_key:
            self.cache[cache_key] = {
                'timestamp': datetime.now().isoformat(),
                'assembly': str(test_assembly),
                'result': result
            }
            self.save_cache()
    
    def clear_cache(self):
        """Clear all cached results"""
        self.cache = {}
        self.save_cache()
        print("✅ Test cache cleared")
    
    def get_cache_stats(self):
        """Get cache statistics"""
        total_entries = len(self.cache)
        valid_entries = sum(1 for entry in self.cache.values() if self.is_cache_valid(entry))
        
        return {
            'total_entries': total_entries,
            'valid_entries': valid_entries,
            'expired_entries': total_entries - valid_entries,
            'cache_size_kb': self.cache_file.stat().st_size / 1024 if self.cache_file.exists() else 0
        }
    
    def prune_expired(self):
        """Remove expired cache entries"""
        original_size = len(self.cache)
        self.cache = {
            key: entry for key, entry in self.cache.items()
            if self.is_cache_valid(entry)
        }
        removed = original_size - len(self.cache)
        
        if removed > 0:
            self.save_cache()
            print(f"✅ Pruned {removed} expired cache entries")
        
        return removed

# Enhanced test runner with caching
class CachedTestRunner:
    def __init__(self):
        self.cache_manager = TestCacheManager()
        self.project_root = Path("/home/ubuntu/neo-service-layer")
    
    def run_test_with_cache(self, test_dll):
        """Run test with caching support"""
        import subprocess
        import re
        
        # Check cache first
        cached_result = self.cache_manager.get_cached_result(test_dll)
        if cached_result:
            print(f"  📦 Using cached result (saved {cached_result['duration_ms']}ms)")
            return cached_result
        
        # Run test if not cached
        print(f"  🔄 Running test (not in cache)")
        start_time = time.time()
        
        try:
            result = subprocess.run(
                ["dotnet", "vstest", str(test_dll), "--logger:console;verbosity=quiet"],
                capture_output=True,
                text=True,
                timeout=120,
                cwd=str(self.project_root)
            )
            
            duration_ms = int((time.time() - start_time) * 1000)
            
            # Parse results
            test_result = {
                'passed': "Passed!" in result.stdout,
                'duration_ms': duration_ms,
                'cached': False
            }
            
            # Extract test counts
            match = re.search(r"Passed:\s*(\d+).*Failed:\s*(\d+).*Total:\s*(\d+)", result.stdout)
            if match:
                test_result['passed_count'] = int(match.group(1))
                test_result['failed_count'] = int(match.group(2))
                test_result['total_count'] = int(match.group(3))
            
            # Cache the result
            self.cache_manager.cache_result(test_dll, test_result)
            
            return test_result
            
        except Exception as e:
            print(f"  ❌ Error: {e}")
            return None
    
    def run_all_tests_with_cache(self):
        """Run all tests with caching"""
        print("=" * 70)
        print("CACHED TEST EXECUTION")
        print("=" * 70)
        
        # Print cache stats
        stats = self.cache_manager.get_cache_stats()
        print(f"Cache Stats: {stats['valid_entries']} valid entries, {stats['cache_size_kb']:.1f}KB")
        print()
        
        # Prune expired entries
        self.cache_manager.prune_expired()
        
        # Find test DLLs
        test_dlls = list(self.project_root.glob("tests/**/bin/Release/net9.0/*.Tests.dll"))
        
        total_time_saved = 0
        cache_hits = 0
        
        for dll in test_dlls[:10]:  # Test first 10
            print(f"\nTesting: {dll.name}")
            result = self.run_test_with_cache(dll)
            
            if result and result.get('cached'):
                cache_hits += 1
                total_time_saved += result['duration_ms']
        
        print("\n" + "=" * 70)
        print("CACHE PERFORMANCE")
        print("=" * 70)
        print(f"Cache Hits: {cache_hits}")
        print(f"Time Saved: {total_time_saved}ms ({total_time_saved/1000:.1f}s)")
        print(f"Cache Hit Rate: {cache_hits/len(test_dlls)*100:.1f}%" if test_dlls else "N/A")

if __name__ == "__main__":
    import sys
    
    if len(sys.argv) > 1:
        if sys.argv[1] == "clear":
            manager = TestCacheManager()
            manager.clear_cache()
        elif sys.argv[1] == "stats":
            manager = TestCacheManager()
            stats = manager.get_cache_stats()
            print(f"Cache Statistics:")
            print(f"  Total Entries: {stats['total_entries']}")
            print(f"  Valid Entries: {stats['valid_entries']}")
            print(f"  Expired: {stats['expired_entries']}")
            print(f"  Size: {stats['cache_size_kb']:.1f}KB")
        elif sys.argv[1] == "prune":
            manager = TestCacheManager()
            removed = manager.prune_expired()
            print(f"Removed {removed} expired entries")
    else:
        # Run tests with caching
        runner = CachedTestRunner()
        runner.run_all_tests_with_cache()