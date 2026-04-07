import sys
import requests
import os
import subprocess
import shutil
from urllib.parse import unquote
import re

def main():
    if len(sys.argv) < 5:
        print("用法: python download.py <url> <title> <video_url> <audio_url> <output_dir> [only_audio|only_video|merge]")
        return

    url = sys.argv[1]
    title = sys.argv[2]
    video_url = sys.argv[3]
    audio_url = sys.argv[4]
    output_dir = sys.argv[5] if len(sys.argv) >= 6 else "."
    mode = sys.argv[6] if len(sys.argv) >= 7 else "merge"

    # 展开 ~ 符号
    output_dir = os.path.expanduser(output_dir)

    # 确保输出目录存在
    if not os.path.exists(output_dir):
        os.makedirs(output_dir)

    headers = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
        "Referer": "https://www.bilibili.com",
        "Origin": "https://www.bilibili.com",
        "Range": "bytes=0-"
    }

    print("开始下载...", flush=True)

    if mode == "only_audio" and audio_url:
        audio_download(audio_url, headers, title, output_dir, url)

    elif mode == "only_video" and video_url:
        video_download(video_url, headers, title, output_dir, url)

    elif mode == "merge" and video_url and audio_url:
        video_file = video_download(video_url, headers, title, output_dir, url)
        audio_file = audio_download(audio_url, headers, title, output_dir, url)
        if video_file and audio_file:
            merge(video_file, audio_file, title, output_dir)

    else:
        print("参数错误")

def video_download(url, headers, title, output_dir, referer):
    headers["Referer"] = referer
    try:
        with requests.get(url, headers=headers, stream=True, timeout=30) as res:
            res.raise_for_status()
            ext = '.mp4'
            base = sanitize_filename(title or 'video')
            filename = unique_filename(base + ext, output_dir)
            filepath = os.path.join(output_dir, filename)
            with open(filepath, 'wb') as f:
                for chunk in res.iter_content(1024 * 1024):
                    if chunk:
                        f.write(chunk)
            print(f"已保存视频: {filepath}")
            return filepath
    except Exception as e:
        print(f"下载视频失败: {e}")
        return None

def audio_download(url, headers, title, output_dir, referer):
    headers["Referer"] = referer
    try:
        with requests.get(url, headers=headers, stream=True, timeout=30) as res:
            res.raise_for_status()
            ext = '.m4a'
            base = sanitize_filename(title or 'audio')
            filename = unique_filename(base + ext, output_dir)
            filepath = os.path.join(output_dir, filename)
            with open(filepath, 'wb') as f:
                for chunk in res.iter_content(1024 * 1024):
                    if chunk:
                        f.write(chunk)
            print(f"已保存音频: {filepath}")
            return filepath
    except Exception as e:
        print(f"下载音频失败: {e}")
        return None

def merge(video_file, audio_file, title, output_dir):
    if not shutil.which('ffmpeg'):
        print('未检测到 ffmpeg，无法合并，请安装 ffmpeg 并确保在 PATH 中')
        return None

    base = sanitize_filename(title or 'merged')
    filename = unique_filename(f"{base}_merged.mp4", output_dir)
    out_name = os.path.join(output_dir, filename)

    cmd = [
        'ffmpeg', '-y',
        '-i', video_file,
        '-i', audio_file,
        '-c:v', 'copy',
        '-c:a', 'aac',
        out_name
    ]

    try:
        subprocess.run(cmd, check=True)
        print(f"合并完成: {out_name}")
        return out_name
    except subprocess.CalledProcessError as e:
        print(f"合并失败: {e}")
        return None

def sanitize_filename(name):
    name = unquote(name)
    name = re.sub(r'[\\/:*?"<>|]', '_', name)
    name = name.strip()
    if not name:
        name = 'file'
    return name

def unique_filename(path, output_dir=None):
    base, ext = os.path.splitext(path)
    counter = 1
    target_dir = output_dir if output_dir else "."
    candidate = os.path.join(target_dir, path)
    while os.path.exists(candidate):
        candidate = os.path.join(target_dir, f"{base}_{counter}{ext}")
        counter += 1
    return candidate

if __name__ == '__main__':
    main()
