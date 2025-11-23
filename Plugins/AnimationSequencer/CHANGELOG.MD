# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

## [2.0.0]
### Changed
- Resolved "GetScriptingDefineSymbols" warning message in Unity 6.
- Fix: Prevented incorrect value assignment at the end of "Punch" and "Shake" animations.
- Fixed an issue with "RectTransformAnchoredPositionTweenAction" incorrectly calculating the end position when the object was rotated. Added the "IncludeBounds" option.
- Fixed an issue with "RectTransformAnchoredPositionTweenAction" calculating the end position incorrectly when used with "TransformScaleTweenAction", "RectTransformSizeDeltaTweenAction", and "TransformRotationTweenAction". As a temporary solution, the "CalculateEndValuesFromOtherActions" code has been commented out.
- Added "BlockRaycastsStep".
- Added "LookAt 2D", "LookAt", "Path" and "Path (RectTransform)" actions.
- Added "AnimationSequencer" to the component menu.
- Refactored and renamed "AnimationSequencerController" to "AnimationSequencer" for improved readability.
- Refactored "ColorTweenAction", "FadeTweenAction", "TextTweenAction", and "TextureScaleTweenAction" classes.
- Refactored Enums.
- Renamed transform action classes for better code readability.
- Renamed "Tween actions" classes for improved clarity.
- Renamed "Steps" classes for better organization.
- Renamed the "MovementDirection" enum to "AnchorPosition", along with its related classes.
- Renamed the "toPercentage" variable to "toPercentageMode" and added a more descriptive tooltip.
- Renamed "End values" variables in steps and tween actions.
- Removed the "Relative" property from tween actions, except for "PositionTweenAction" and "RT_AnchoredPositionTweenAction".

## [1.5.0]
### Changed
- Fixed issues with "SampleScene" values (AutoplayType).
- Added "dynamicStartValues" option in inspector. If true, replayed animations use current object values as the new start values; otherwise, the original start values are reused.
- Updated "PlayParticleSystemAnimationStep" and "SetGameObjectActiveStep" logic.
- Fixed reset behavior for text tweens.
- Fixed bug where a paused tween in backward mode reset its position upon resuming.
- Added "PlayBackwards" button to editor view.
- Fixed functionality of "StepBack" and "StepNext" editor buttons.
- Enhanced editor usability:
    - Added "OnlyOneActionExpandedWhileEditing" feature.
    - Added "Action" foldout for better space management.
    - Added "OneStepExpandedWhileEditing" feature.
    - Added "CollapseStepsWhenPreviewing" feature.
- Improved visuals in the editor:
    - Customized "AnimationSequencer" preferences window.
    - Updated animation step visualization in the editor.
    - Refined "Preview" panel layout.
    - Adjusted styles for steps in editor view.
- Improved functionality and options:
    - Added "LookAt(3D/2D)" and "RectTransformPath" tween actions.
    - Fixed AxisConstraint for "position" and "scale" tween actions, allowing axis combinations like X and Y.
    - Added default value for action ease.
    - Added descriptive tooltips to relevant variables.
    - Reordered settings and events properties in the editor.
    - Reorganized "AutoplayType" enum values.
- Enhanced coding quality:
    - Added regions to the "AnimationSequencerController" class.
    - Cleaned up "AnimationSequencerControllerCustomEditor."
- Resolved editor-specific issues:
    - Fixed "Progress" slider refresh when entering play mode.
    - Saved "Steps" foldout state (extended or collapsed).
    - Fixed glitch when deleting actions in editor view.
    - Addressed issues when reordering the steps list.
    - Added "ShowIf" attribute to conditionally hide certain properties.
    - Added descriptive "LogWarnings" for steps and actions.
    - Polished editor visuals (removed extra spaces, adjusted containers, reduced margins).

## [1.0.1]
### Changed
- Added a sample scene that demonstrate the usage of the Animation Sequencer.
- Fixed issue with "Size Delta" tween when RectTransform was set to stretched.
- Added an option to use the input value as a percentage for "Orthographic Size", "Size Delta" and "Scale" tween actions.
- Added an option in the settings menu on `Edit/Preferences/AnimationSequencer` to show or not the animation data for each step in the inspector like the blue progress bar.

## [1.0.0]
### Changed
- Update: Avoid adding a tween action to the same step more than once.
- Added "Anchor" type input to the "Anchored Position" tween action to add the ability to move the target to a specific parent anchor (TopLeft, TopCenter, TopRight, MiddleLeft, MiddleCenter, MiddleRight, BottomLeft, BottomCenter, BottomRight).
- Update: Hide unnecessary fields in the inspector.
- Added new tween actions: Audio Source (Volume, Pitch), Camera (Color, Field Of View, Orthographic Size), Graphic (Texture Offset, Texture Scale), Light (Color, Intensity), Renderer (Texture Offset, Texture Scale), Slider (Value), SpriteRenderer (Color), Text (Text, Counter).
- Tween actions refactoring.

## [0.5.6]
### Changed
- Fixed issue with "Move to RectTransform Anchored Position" and "Move To Anchored Position" tween. The destination target was calculated wrong if the current object has a scale other than one.
- Fixed issue with CustomEditor size when the step animation is of type "Join".
- Update: "Animation Sequence Settings" and "Sequence Settings" panels mixed.
- Added a progress bar in the inspector. This bar contains data about the animation of each step.
- Added information in editor preview about the duration of the sequence.
- Fixed issue with some animation steps when running in "Backward" direction.
- Code Cleanup: Removed extra methods from the "Progress" slider. This also fixed an issue in the editor when exiting "EditingPrefab" in unity 2021.

## [0.5.4]
### Changed
- Fixed issue when using "EditorPreview" and exiting "EditingPrefab". Bug: "MissingReferenceException: The object of type 'GameObject' has been destroyed but you are still trying to access it".
- Fixed issue where the "AnimationSequencerController" loses focus: Now when this happens, target values and EditorPreview settings are reset.
- Fixed issue when tween target was changed and not taken into account.
- Fixed issue with UI objects tweens; in EditorPreview, UI elements were not updating correctly.
- Fixed issue when using color tween and scale tween at the same time in editor mode.
- Update: When the "PlayForward" or "PlayBackwards" functions are executed one after the other they interrupt each other to give priority to the last function called, however if these functions are called themselves multiple times nothing happens unless the animation has been completed, in this last case the animation restarts and runs.
- Fixed issue where "PlayForward" function didn´t work when called from code.
- Fixed issue where "Backwards" animation didn´t work in both editor or play mode.
- Fixed issue where "PlayBackwards" function didn´t work when called from code.
- Fixed issue with "SetProgress" function: The progress value was not being calculated correctly.
- Improved "Move To Anchored Position" tween with "local" paramenter to move the object to local or global position in Canvas.
- Fixed issue with "Move to RectTransform Anchored Position" tween. The destination target is now calculated based on the global position instead of local position based on the canvas.
- Update: "Code formatting" applied to steps and actions.
- Fixed warning about "Move to RectTransform Anchored position" action. Warning: "The same field name is serialized multiple times in the class or its parent class. This is not supported: Base(AnchoredPositionMoveToRectTransformPositionDOTweenActionBase) rectTransform".
- Fixed bug: AutoplayType changed from Awake to Start. Sometimes in a "move tween" action the target values are taken wrong because the AnimationSequencerController runs before the UI controls.

## [0.5.1]
### Changed
- Fixed issue when previewing animation and having the `AutoHideWhenPreviewing` enabled would cause the Steps not expand on the second play
- Fixed issue where FadeCanvas didn't work while previewing on Editor on Unity 2021

## [0.5.0]
### Changed
- Added support to control AnimationSequencer though regular unity animations, in both runtime / playtime
- Fixed issue with color tweens no working on the editor > 2021
- Added a settings menu on `Edit/Preferences/AnimationSequencer`
- You can now disable the auto foldout of the steps while previewing on the Settings

## [0.4.0]
### Changed
- Fixed Unity 2020 compability

## [0.3.9]
### Changed
- Fixed Unity 2021 support
- Added a new setup to help dealing with projects when DOTween is not available. The AnimationSequencer will try to find the DOTween asmdef and add a scripting defining symbol `DOTWEEN_ENABLED`. Keep in mind that if you remove DOTWEEN you will have to remove the asmdef by hand. 

## [0.3.8]
### Changed
- Fixed Unity 2020 foldout issues
- Added other small null checks

## [0.3.5]
### Changed
- Fixed Infinite Loops not working with sequence, well its not fixed but I'm using the workaround described here: https://github.com/Demigiant/dotween/issues/92
- Exposed all the Steps/Actions changeable values as properties so they can be changed by code.
- Added new `TryGetStepAtIndex` and `TryGetActionAtIndex` to be able to get and manipulate values from code.

## [0.3.4]
### Changed
- Fixed InvokeCallbackStep adding the callback to the parent sequence
- Fixed not allowing -1 as loop number for the DOTweenStep

## [0.3.3]
### Changed
- Fixed issue in which the Animation Step's Delay was being applied to each Action individually, this was causing them to execute sequentially rather than simultaneously.
- Fixed an issue in which joined Steps after an Invoke Callback Step with a delay were incorrectly delayed.
- Added general defaults, previously defaults could only be set for Actions.
- Worked around a Unity bug in which updating the colour of a Graphic does not cause any visual change outside of PlayMode (affects "Color Graphic" and "Fade Graphic" actions).
- Improved Step naming for Invoke Callback to include the called function name and also support multiple callbacks.
- Added RectTransformSizeDOTweenAction to tween the sizeDelta of a Graphic.
- Fixed a bug in which exiting the prefab staging view with an active preview would cause a null ref and failure to reset the animation. This was due to the timing of EditorWIndow.OnDisable (it is is called after the prefab objects have already been destroyed) and has been fixed by hooking into PrefabStage.prefabSaving.
- Added runtime playback speed.


## [0.3.2]
### Changed
- Fixed bug with delay and multiple sequences
- Expand the steps panel if started playing with the panel open.~~~~

## [0.3.1]
### Changed
- Added `AutoplayType`, this allows animations to trigger in either `Awake` or `OnEnable` (defaults to Awake to retain backwards compatibility)
- Fixed issues with backward playback in Editor Preview Tool and at runtime.
- Fixed callbacks not firing correctly when playing backward.
- If `AutoplayType` is OnEnable, then `OnDisable` will kill the tweens and reset the animated objects to their initial states, this ensures that they are in the right state for the Tweens to be regenerated the next time the animation is enabled.
- TimeScale slider in Preview Tool can now be used before playback commences.
- Exposed getters for `PlayOnAwake` and `PauseOnAwake`, these can be used for asserts to catch incorrect setup of `AniamtionSequenceControllers` from the code's perspective.
- Fixed an issue with "Move To Anchored Position" when using the IsRelative flag. It must be set when calling tween.From(isRelative) for it to work.
- Fixed delays not working for Callback steps.
- Fixed potential null refs in various DOTweenActions.
- Allowed various parts of AnimationSequenceController and AnimationSequenceControllerCustomEditor to be overridden.
- Play On Awake and Pause On Awake now alter their inspector label dependent on the selected AutoplayType.
- Fixed a bug with the completion callbacks passed to Play, PlayForward and PlayBackwards, old callbacks were executing for subsequent plays. All listeners are now removed each time any playback function is executed.
- Fixed typo in PlayForward bool.

Thanks for @nindim for all the improvements and bug fixes

## [0.3.0]
### Changed
- Exposed `AnimationSequencerController` methods to be virtual, so they can be extended.
- Removed the `sealed` from `AnimationSequencerControllerCustomEditor` so allow custom editors as well.
- Refactor the Play / Forward / Backward setup, to be easier to understand and use
- Added `autoKill` setting on the sequence settings, to allow back and forward play easily;

## [0.2.8]
### Changed
- Added the SetProgress to be able to set the position of the sequencer by code.
- Set the `Play` method to virtual so can be overwritten 
- Added [DOTWeen from the UPM](https://openupm.com/packages/com.demigiant.dotween/) package dependency 
- Fixed Timescale slider greyed out before editor was playing
- Added direction as a parameter of the `Play` method, so you can trigger to play backwards and forward by code.

## [0.2.7]
### Changed
- [Fixed issue](https://github.com/brunomikoski/Animation-Sequencer/pull/29) when `OnStart` event was been triggered inside the `OnComplete` event. [JohnDesley](https://github.com/JohnDesley)
- Automatically hide the Steps Panel when previewing to increase performance.

## [0.2.6]
### Changed
 - Removed the `Play` from the playtype, now is only `Backward` and `Forward` and `Forward` is the default one
 - Implement manual reset of editor changes to avoid issues while previewing animation and stop
 - Refactored the preview controllers to allow all the controllers to be able to play the animation when needed
 - Improved inspector performance 

### Added
 - Implemented a bunch of feedback from [nindim](https://github.com/nindim)

## [0.2.5]
### Changed
- Replaced TypeUtility for [Unity TypeCache](https://docs.unity3d.com/2019.4/Documentation/ScriptReference/TypeCache.html)

## [0.2.4]
### Changed
- Fixed issue of Join sequences 

## [0.2.3]
### Changed
- Fixed issue with Append / Join steps

## [0.2.2]
### Changed
- Updated Readme
- Update Delay implementation 
- Removed Duration variable.
- Small cleanups

## [0.2.1]
### Added
- Added ColorGraphic DOTween step.
- While previewing if you press play again with a finished tween, will rewind it first
- Added loops settings for the entire sequence.
- More warnings and help information boxes while previewing
- Added Sequence Settings Panel to control DOTween Sequence Settings only 
 
### Changed
- Disabled infinity loops to be added to DOTween Steps, [More Information](https://github.com/brunomikoski/Animation-Sequencer/issues/19#issuecomment-895668338)
- Cached components on Tween Step to improve performance.
- The Sequence will be killed if the animation controller is destroyed

## [0.2.0]
Thanks for all the [suggestions](https://github.com/brunomikoski/Animation-Sequencer/issues/16) @nindim
### Changed
- Refactored the system to use Sequences as core, instead of proprietary implementation
- Changed the preview controls
- Fixed chain of sequences to be played properly on editor.
- Changed how the DOTweenEditorPreview works

### Added
- Added UnityEvents callback to be exposed on the editor.
- Added TimeScale slider to control preview
- Added progress slider to scrubing through the sequence
- Added new exposed settings (`UpdateType` `TimeScale Independent`, `Auto Kill`)
- Added new play type
- Added `AnimationControllerDefaults` where you can set some default values and behaviours

### Requirement
- Requires latest DOTween version (v1.2.632)

## [0.1.5]
### Changed
- Fixed one issue that prevent you from playing the same animation twice on editor
- Limited the `-1 (Infinity lopps)` on editor playback, this was causing some issues so will show a Warning and limit to 3 loops if was set to -1
- Fixed issue when trying to complete the Sequence on editor, now only Stop is available, and will complete all the sequence.


## [0.1.4]
### Added
- Added the `Invoke Callback Step` that uses `Unity Events` to trigger callbacks inside one sequence! Thanks @VladmirCSouza

## [0.1.3]
### Added
- Added Step Finished / Step Begin events that can be subscribed on the `Animation Controller`

### Changed
- Fixed issue when a custom step would have a null `DisplayName`

## [0.1.2]
### Added
- Added `Rewind()` functionality, thanks @qwe321

### Changed
- Fixed DOTween editor been used on runtime platform

## [0.1.1]
### Added 
- Added Complete method for the Sequence, so you can force one sequence to finished instantaneous
- Added the `force` parameter on the `PrepareToPlay` method, to execute the preparation of a step even if is already generated
- Added initialization settings for the `Animation Sequencer`

### Fixed
- Fixed issue where the duration of a sequence composed of multiple sequences would not sore the correct duration

## [0.1.0]
### Added 
- First initial working version

[2.0.0]: https://github.com/PabloHuaxteco/AnimationSequencer/releases/tag/v2.0.0
[1.5.0]: https://github.com/PabloHuaxteco/Animation-Sequencer/releases/tag/v1.5.0
[1.0.1]: https://github.com/PabloHuaxteco/Animation-Sequencer/releases/tag/v1.0.1
[1.0.0]: https://github.com/PabloHuaxteco/Animation-Sequencer/releases/tag/v1.0.0
[0.5.6]: https://github.com/PabloHuaxteco/Animation-Sequencer/releases/tag/v0.5.6
[0.5.4]: https://github.com/PabloHuaxteco/Animation-Sequencer/releases/tag/v0.5.4
[0.5.1]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.5.1
[0.5.0]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.5.0
[0.4.0]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.4.0
[0.3.9]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.3.9
[0.3.8]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.3.8
[0.3.6]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.3.6
[0.3.5]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.3.5
[0.3.4]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.3.4
[0.3.3]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.3.3
[0.3.2]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.3.2
[0.3.1]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.3.1
[0.3.0]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.3.0
[0.2.8]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.2.8
[0.2.7]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.2.7
[0.2.6]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.2.6
[0.2.5]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.2.5
[0.2.4]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.2.4
[0.2.3]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.2.3
[0.2.2]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.2.2
[0.2.1]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.2.1
[0.2.0]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.2.0
[0.1.5]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.1.5
[0.1.4]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.1.4
[0.1.3]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.1.3
[0.1.2]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.1.2
[0.1.1]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.1.1
[0.1.0]: https://github.com/brunomikoski/Animation-Sequencer/releases/tag/v0.1.0
