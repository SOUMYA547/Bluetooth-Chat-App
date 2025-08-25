# Scans nearby BLE devices using Bleak and prints JSON to stdout
# Usage: python BluetoothScanner.py --timeout 6
import asyncio, json, argparse
from bleak import BleakScanner

async def main(timeout: float):
    devices = await BleakScanner.discover(timeout=timeout)
    out = []
    for d in devices:
        out.append({
            "name": d.name or "(unknown)",
            "address": d.address,
            "rssi": getattr(d, "rssi", None)
        })
    print(json.dumps({"devices": out}, ensure_ascii=False))

if __name__ == "__main__":
    p = argparse.ArgumentParser()
    p.add_argument("--timeout", type=float, default=5.0)
    args = p.parse_args()
    asyncio.run(main(args.timeout))
