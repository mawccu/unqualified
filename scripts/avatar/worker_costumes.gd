class_name WorkerCostumes
extends RefCounted
## The four coworkers. One shared body; each differs in colour block, accessories
## and hair so they read apart at ten metres. All original, all placeholder.
## "Almost looks professional": a vest or blazer, a clip-on tie, a headset, a
## lanyard, one glove, a radio. The player slot picks the costume.

const VARIANTS: Array[Dictionary] = [
	{
		"name": "Red vest",
		"shirt": Color(0.96, 0.95, 0.92), "vest": Color(0.82, 0.14, 0.18), "trousers": Color(0.12, 0.12, 0.15),
		"shoes": Color(0.97, 0.97, 0.97), "skin": Color(0.93, 0.74, 0.60), "hair": Color(0.25, 0.14, 0.09),
		"accent": Color(0.82, 0.14, 0.18), "hair_style": &"short",
		"accessories": [&"bow_tie", &"lanyard"],
	},
	{
		"name": "Blue blazer",
		"shirt": Color(0.88, 0.92, 0.97), "vest": Color(0.16, 0.34, 0.78), "trousers": Color(0.10, 0.11, 0.16),
		"shoes": Color(0.95, 0.95, 0.95), "skin": Color(0.66, 0.47, 0.34), "hair": Color(0.09, 0.07, 0.07),
		"accent": Color(0.16, 0.34, 0.78), "hair_style": &"pompadour",
		"accessories": [&"headset", &"radio"],
	},
	{
		"name": "Yellow apron",
		"shirt": Color(0.95, 0.93, 0.85), "vest": Color(0.30, 0.30, 0.34), "trousers": Color(0.14, 0.14, 0.17),
		"shoes": Color(0.97, 0.9, 0.55), "skin": Color(0.98, 0.82, 0.68), "hair": Color(0.88, 0.62, 0.22),
		"accent": Color(0.98, 0.80, 0.14), "hair_style": &"ponytail",
		"accessories": [&"apron", &"lanyard"],
	},
	{
		"name": "Green utility",
		"shirt": Color(0.90, 0.95, 0.90), "vest": Color(0.20, 0.62, 0.34), "trousers": Color(0.13, 0.13, 0.15),
		"shoes": Color(0.97, 0.97, 0.97), "skin": Color(0.80, 0.58, 0.42), "hair": Color(0.15, 0.10, 0.08),
		"accent": Color(0.20, 0.62, 0.34), "hair_style": &"beanie",
		"accessories": [&"belt", &"one_glove"],
	},
]


static func for_slot(slot: int) -> Dictionary:
	return VARIANTS[posmod(slot, VARIANTS.size())]
