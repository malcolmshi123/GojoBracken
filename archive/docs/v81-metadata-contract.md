# v81 metadata contract

Source: the user-supplied offline-managed-metadata report, generatedUtc 2026-10-26T03:32:58.7296778Z. Its game assembly MVID matches the earlier testing-PC startup log:

- MVID: aca1e98d-6f84-4d3f-85cd-22b6f7be2f9b
- Reported assembly SHA256: 5F7DB5538B78DC408845A3002907619785AC9F9C6B6059D13DC9A602D9B65731
- Assembly: Assembly-CSharp, Version=0.0.0.0
- Separately reported main-menu version: 81

The original report is retained locally under ignored artifacts/offline-v81-metadata.json. No game DLL or full metadata dump is committed.

## Confirmed declarations and references

| Member | Declaring type | Declared type / report evidence |
| --- | --- | --- |
| currentBehaviourStateIndex | EnemyAI | Int32; written by SwitchToBehaviourStateOnLocalClient |
| angerMeter | FlowermanAI | Single |
| isInAngerMode | FlowermanAI | Boolean; Update writes it |
| carryingPlayerBody | FlowermanAI | Boolean; body-drop and kill-completion methods write it |
| inKillAnimation | FlowermanAI | Boolean; kill RPC/completion methods write it |
| isEnemyDead | EnemyAI | Boolean |
| creatureAnimator | EnemyAI | UnityEngine.Animator |
| animationContainer | FlowermanAI | UnityEngine.Transform |
| rightHandGrip | FlowermanAI | UnityEngine.Transform |
| skinnedMeshRenderers | EnemyAI | UnityEngine.SkinnedMeshRenderer[] |
| meshRenderers | EnemyAI | UnityEngine.MeshRenderer[] |

FlowermanAI.CalculateAnimationDirection references Animator.SetFloat and contains VelocityX/VelocityZ string literals. Update references SetBool/SetFloat and contains anger, sneak, and speedMultiplier literals. DropPlayerBody/FinishKillAnimation contain carryingBody; kill RPC/completion methods contain killing. These are candidate animation parameter names; this export does not preserve the argument-to-call association.

EnterAngerModeServerRpc(Single) references EnterAngerModeClientRpc(Single). The latter writes angerMeter and references SwitchToBehaviourStateOnLocalClient. This evidence means a blanket claim that anger is never synchronized is not justified. It does not establish exactly what value is assigned, continuous synchronization, or how remote clients update anger between events.

## Prepared read-only adapter

shared/GojoBracken.Presentation/Runtime/BrackenStateReader.cs binds the six observation fields by name and exact CLR type, including inherited EnemyAI fields. It returns a raw snapshot of state index, anger, anger-mode flag, carrying flag, kill-animation flag, and death flag. It does not keep enemy references, write fields, clamp an unverified anger range, translate state indices into phases, or infer ownership/authority.

Missing/wrong fields and null/wrong-type instances return an unavailable result. NaN/infinite anger is rejected so later presentation code can fall back safely. Separate instances do not share presentation state.

The reader now feeds the controller through a default-off [development effects adapter](development-effects.md). The default installation still leaves gameplay and visuals unchanged; actual game validation remains pending.

Verification:

~~~powershell
dotnet run --project tests/StateReaderChecks/StateReaderChecks.csproj -c Release
dotnet build GojoBracken.sln -c Release --no-restore
~~~

Checks passed for inherited fields, unmodified raw state/anger values, overlapping flags, independent instances, no mutation, nonfinite inputs, and incompatible schemas. The plugin build passed with zero warnings/errors. The check project uses the installed .NET 10 SDK; the plugin target remains netstandard2.1.

## What still needs inspection

This report lists calls, writes, and literals as unordered sets. It cannot establish the state switch cases, conditions around isInAngerMode, anger limits or thresholds, the value stored by the anger RPC, or whether owner/client branches differ. Do not guess numeric phases or choose an authoritative anger maximum from it.

The current Assembly-CSharp.dll has now been supplied privately. Its MVID and SHA256 both matched, and its method bodies were inspected without loading/executing the game assembly. See [verified v81 behavior and prepared cosmetic rules](v81-behavior.md). The unordered report limitations above describe the original metadata report, not the later DLL inspection. Keep the game closed and leave diagnostics disabled; no further launch or encounter is requested.

Prefab bone hierarchy, render-pipeline configuration, real material compatibility, animation adaptation, and mixed-client integration remain separate pending work.
