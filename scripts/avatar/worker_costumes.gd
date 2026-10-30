class_name WorkerCostumes
extends RefCounted
## The four coworkers: one shared uniform language ("budget luxury event staff")
## with small differences, so they read apart by silhouette first and by a single
## small accent colour second. Charcoal trousers, off-white shirts, a dark vest
## or apron, muted skin and hair. Saturated colour is reserved for interaction
## feedback and UI, not for the workers.
##
## The same table drives the code-built fallback and any authored .glb worker:
## the model's materials named Shirt, Vest, Trousers, Shoes, Skin, Hair and
## Accent are tinted from here, and its Hair_<style> mesh is the one shown.

const VARIANTS: Array[Dictionary] = [
	{
		"name": "Charcoal vest, burgundy pocket square",
		"shirt": Color(0.92, 0.90, 0.85), "vest": Color(0.20, 0.20, 0.23), "trousers": Color(0.15, 0.15, 0.17),
		"shoes": Color(0.14, 0.13, 0.13), "skin": Color(0.85, 0.69, 0.57), "hair": Color(0.22, 0.14, 0.10),
		"accent": Color(0.55, 0.14, 0.20), "hair_style": &"short",
		"accessories": [&"bow_tie", &"lanyard"],
	},
	{
		"name": "Navy blazer, brass badge",
		"shirt": Color(0.90, 0.91, 0.92), "vest": Color(0.17, 0.20, 0.28), "trousers": Color(0.14, 0.14, 0.17),
		"shoes": Color(0.14, 0.13, 0.13), "skin": Color(0.60, 0.43, 0.32), "hair": Color(0.10, 0.08, 0.08),
		"accent": Color(0.75, 0.60, 0.28), "hair_style": &"pompadour",
		"accessories": [&"headset", &"radio"],
	},
	{
		"name": "Brown apron, teal lanyard",
		"shirt": Color(0.93, 0.91, 0.86), "vest": Color(0.30, 0.24, 0.20), "trousers": Color(0.15, 0.15, 0.17),
		"shoes": Color(0.18, 0.15, 0.13), "skin": Color(0.90, 0.74, 0.62), "hair": Color(0.62, 0.45, 0.25),
		"accent": Color(0.25, 0.52, 0.52), "hair_style": &"ponytail",
		"accessories": [&"apron", &"lanyard"],
	},
	{
		"name": "Forest vest, rose pin",
		"shirt": Color(0.91, 0.92, 0.88), "vest": Color(0.18, 0.28, 0.22), "trousers": Color(0.14, 0.14, 0.16),
		"shoes": Color(0.14, 0.13, 0.13), "skin": Color(0.74, 0.55, 0.42), "hair": Color(0.16, 0.11, 0.09),
		"accent": Color(0.78, 0.45, 0.50), "hair_style": &"beanie",
		"accessories": [&"belt", &"one_glove"],
	},
]


static func for_slot(slot: int) -> Dictionary:
	return VARIANTS[posmod(slot, VARIANTS.size())]
