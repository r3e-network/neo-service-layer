#include "StorageManager.h"
#include <cstdio>
#include <cstring>
#include <sys/stat.h>
#include <sys/types.h>
#include <fcntl.h>
#include <unistd.h>
#include <dirent.h>
#include <fstream>
#include <sstream>
#include <nlohmann/json.hpp>

using json = nlohmann::json;

std::string StorageManager::get_namespace_path(const std::string& namespace_id)
{
    return _storage_path + "/" + namespace_id;
}

std::string StorageManager::get_file_path(const std::string& namespace_id, const std::string& key)
{
    return get_namespace_path(namespace_id) + "/" + key;
}
