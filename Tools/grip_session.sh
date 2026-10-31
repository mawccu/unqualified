#!/usr/bin/env bash
# Start a wedding session in the open Editor and pick up a prop: grip_session.sh <prop id>
cd "$(dirname "$0")/.."
source Tools/shift_driver.sh
ucmd script eval --code 'var c = CrewState.Current; c.job = Jobs.Wedding; c.Save(); UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Wedding.unity"); return "ok";' >/dev/null
ucmd editor.playmode start >/dev/null; sleep 12
step "near $1 0.7"; sleep 0.3; step "pickup $1"; sleep 1.5
