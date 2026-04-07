import sys
import requests
import re
import json

def format_bandwidth(bw):
    """格式化带宽为人类可读格式"""
    if bw >= 1000000:
        return f"{bw // 1000000} Mbps"
    elif bw >= 1000:
        return f"{bw // 1000} Kbps"
    else:
        return f"{bw} bps"

def main():
    if len(sys.argv) < 2:
        print(json.dumps({"error": "参数错误: 需要 URL 参数"}, ensure_ascii=False))
        return

    url = sys.argv[1]
    cookie = sys.argv[2]
    m = re.search(r'"(.*)"', cookie)

    if m:
        cookie = m.group(1)

    headers = {
        "User-Agent": "Mozilla/5.0",
        "Referer": url,
        "Origin": "https://www.bilibili.com",
        "Range": "bytes=0-"
    }

    if cookie:
        headers["Cookie"] = cookie

    try:
        resp = requests.get(url, headers=headers, stream=True)
        resp.raise_for_status()

        html = resp.text
        m = re.search(r"window\.__playinfo__\s*=\s*(\{.*?\})\s*</script>", html, re.S)
        if not m:
            print(json.dumps({"error": "未找到视频信息"}, ensure_ascii=False))
            return

        data = json.loads(m.group(1))

        audios = data.get('data', {}).get('dash', {}).get('audio', [])
        videos = data.get('data', {}).get('dash', {}).get('video', [])

        result = {
            "videos": [],
            "audios": [],
            "title": extract_title(html, data)
        }

        # 视频信息 - 按带宽排序
        for v in sorted(videos, key=lambda x: x.get('bandwidth', 0), reverse=True):
            result["videos"].append({
                "id": v["id"],
                "bandwidth": v["bandwidth"],
                "bandwidth_text": format_bandwidth(v["bandwidth"]),
                "width": v["width"],
                "height": v["height"],
                "url": v.get("baseUrl") or v.get("base_url")
            })

        # 音频信息 - 按带宽排序
        for a in sorted(audios, key=lambda x: x.get('bandwidth', 0), reverse=True):
            result["audios"].append({
                "id": a["id"],
                "bandwidth": a["bandwidth"],
                "bandwidth_text": format_bandwidth(a["bandwidth"]),
                "url": a.get("baseUrl") or a.get("base_url")
            })
        print(json.dumps(result, ensure_ascii=False))
    except Exception as e:
        print(json.dumps({"error": f"未知错误: {str(e)}"}, ensure_ascii=False))

def extract_title(html, data):
    # try og:title
    m = re.search(r'<meta\s+property="og:title"\s+content="([^"]+)"', html)
    if m:
        return m.group(1)

    # try <title>
    m = re.search(r'<title[^>]*>(.*?)</title>', html, re.S)
    if m:
        return m.group(1).strip()

    # try from JSON data
    try:
        vd = (data or {}).get('data', {})
        for key in ('title', 'name'):
            if key in vd:
                return vd[key]
    except Exception:
        pass

    return 'bilibili'

if __name__ == '__main__':
    main()
