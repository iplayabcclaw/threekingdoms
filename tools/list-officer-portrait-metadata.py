#!/usr/bin/env python3
"""Print portrait-generation metadata from the Godot scenario data."""

from __future__ import annotations

import json
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "godot/data/first-rivalry.json"

FACTION_PALETTES = {
    "faction-liu-bei": "muted ochre, forest green and warm brown",
    "faction-cao-cao": "charcoal, blackened iron and restrained deep red",
    "faction-yuan-shao": "deep blue, aged gold and earthen brown",
    "faction-sun-ce": "river blue, russet red and weathered bronze",
    "faction-liu-biao": "forest gray, moss green and muted blue",
    "faction-lu-bu": "smoky black, dark iron and restrained crimson",
    "faction-ma-teng": "sand ochre, frontier blue and worn leather",
    "faction-gongsun-zan": "cold gray, off-white and northern blue",
    "faction-li-jue": "dusty brown, blackened iron and faded red",
    "faction-yuan-shu": "deep purple, dark bronze and muted gold",
    "faction-zhang-xiu": "burgundy, charcoal and worn bronze",
    "faction-liu-zhang": "teal, warm ochre and dark wood",
    "faction-zhang-lu": "off-white, umber and muted indigo",
    "faction-kong-rong": "scholar indigo, warm gray and aged bronze",
    "faction-shi-xie": "southern ochre, deep green and warm bronze",
    "faction-gongsun-du": "frost gray, dark blue and weathered iron",
    "faction-free": "neutral charcoal, muted earth and restrained jade",
}


def age_direction(age: int) -> str:
    if age < 18:
        return f"approximately {age} years old, clearly adolescent, youthful proportions, smooth face, no beard, not an adult man"
    if age < 25:
        return f"approximately {age} years old, young adult, youthful skin and features, little or no beard"
    if age < 35:
        return f"approximately {age} years old, mature young adult, subtle adult facial definition, no aging wrinkles"
    if age < 45:
        return f"approximately {age} years old, convincingly middle-aged, mature facial structure with subtle expression lines, not elderly"
    if age < 55:
        return f"approximately {age} years old, older middle-aged, visible forehead and eye lines, some gray at the temples or beard, not frail"
    return f"approximately {age} years old, clearly elderly, silver-gray hair and beard, deep natural wrinkles, dignified but not frail"


def role_direction(officer: dict) -> str:
    abilities = officer["abilities"]
    appointment = officer["appointment"]
    if appointment == "ruler":
        return "regional ruler and statesman-warrior"
    if appointment == "civil":
        return "civil official, administrator and diplomat"
    if appointment == "strategist":
        return "military strategist and scholar-official"
    if appointment == "general" or abilities["might"] >= 82:
        return "field commander and martial officer"
    if appointment == "governor" and abilities["politics"] > abilities["might"] + 12:
        return "provincial governor and scholar-official"
    if appointment == "governor":
        return "governor-general balancing civil and military duties"
    if abilities["intelligence"] + abilities["politics"] > abilities["leadership"] + abilities["might"]:
        return "unattached scholar and adviser"
    return "unattached martial officer"


def subject_direction(officer: dict) -> str:
    abilities = officer["abilities"]
    appointment = officer["appointment"]
    if appointment == "ruler":
        return "authoritative layered Han robes over refined practical lamellar armor, understated noble belt, sheathed straight sword"
    if appointment == "civil":
        return "historically grounded Han scholar-official robes, holding bamboo documents or a formal audience tablet, no battlefield weapon"
    if appointment == "strategist":
        return "refined Han scholar-general robes, holding a rolled map or bamboo campaign plans, only a modest sheathed sword if appropriate"
    if appointment == "general" or abilities["might"] >= 82:
        if abilities["might"] >= 93:
            return "powerful athletic build, sturdy historically plausible Han lamellar armor, complete polearm or heavy saber appropriate to an elite warrior"
        if abilities["leadership"] >= abilities["might"]:
            return "disciplined commander build, practical Han lamellar armor, complete spear or sheathed command sword"
        return "strong martial build, weathered Han lamellar armor, complete spear, saber or bow appropriate to a field officer"
    if appointment == "governor":
        return "layered governor robes over light practical Han armor, holding a document case or sheathed command sword according to military ability"
    if abilities["intelligence"] + abilities["politics"] > abilities["leadership"] + abilities["might"]:
        return "plain but refined traveler-scholar robes, bamboo scroll or map case, no heavy armor"
    return "practical light Han armor and travel cloak, sheathed sword or simple spear"


def mood_direction(traits: list[str]) -> str:
    if "豪勇" in traits:
        return "bold and formidable"
    if "善谋" in traits:
        return "observant and strategically composed"
    if "善政" in traits or "爱民" in traits:
        return "humane and cultivated"
    if "多疑" in traits:
        return "guarded and watchful"
    if "野心" in traits:
        return "confident and ambitious"
    return "steadfast and disciplined"


def portrait_prompt(officer: dict) -> str:
    palette = FACTION_PALETTES.get(officer["factionId"], FACTION_PALETTES["faction-free"])
    return f"""Use case: historical-scene
Asset type: full-body officer portrait for a premium Three Kingdoms strategy game
Primary request: an original, historically grounded depiction of {officer['name']}, courtesy name {officer['courtesyName']}, in the year 194 during the late Eastern Han
Historical age direction: born {officer['birthYear']}, {age_direction(officer['age'])}; the apparent age must match this instruction
Historical role: {role_direction(officer)}; reflect the person's established historical reputation where known without theatrical caricature
Subject: {subject_direction(officer)}; expression should feel {mood_direction(officer['traits'])}
Style/medium: highly realistic cinematic historical portrait, natural age-appropriate skin texture, believable Han fabric leather wood and metal, restrained painterly finish matching one coherent premium strategy-game series
Composition/framing: vertical 2:3, one complete person, full body head to toe including both feet and all held equipment, centered three-quarter standing pose, generous margin
Scene/backdrop: restrained dark painterly backdrop using {palette}, with subtle Chinese ink-wash atmosphere and no scenery clutter
Lighting/mood: soft directional historical-drama lighting, dignified and grounded
Constraints: original face design; no resemblance to film or television actors or living people; plausible late Eastern Han attire and equipment; anatomically correct hands; age must read as {officer['age']}; no text, logo, card frame, UI or watermark
Avoid: anime, idol styling, wrong age, generic identical face, fantasy armor, imperial costume unless the subject is emperor, Ming or Qing clothing, Japanese armor, Western armor, cropped head or feet, extra fingers or limbs"""


def main() -> None:
    requested_slugs = set(sys.argv[1:])
    officers = []
    scenario = json.loads(SOURCE.read_text())
    for entry in scenario["officers"]:
        profile = entry["profile"]
        state = entry["initialState"]
        officer_id = profile["id"]
        officer_metadata = {
            "id": officer_id,
            "slug": officer_id.removeprefix("officer-"),
            "name": profile["name"],
            "courtesyName": profile["courtesyName"],
            "birthYear": profile["birthYear"],
            "age": scenario["year"] - profile["birthYear"],
            "abilities": profile["abilities"],
            "traits": profile["traits"],
            "factionId": state["factionId"] or "faction-free",
            "cityId": state["cityId"],
            "appointment": state["appointment"],
        }
        officer_metadata["prompt"] = portrait_prompt(officer_metadata)
        if not requested_slugs or officer_metadata["slug"] in requested_slugs:
            officers.append(officer_metadata)
    print(json.dumps(officers, ensure_ascii=False))


if __name__ == "__main__":
    main()
