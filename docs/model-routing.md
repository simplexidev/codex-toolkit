# Model routing

config/model-routing.json recommends a cheap tier for mechanical read-only work, a
routine coding tier, and stronger reasoning for hard debugging/design. Native agents
set explicit model and reasoning values; verify account access before use. Edit their
TOML or remove model/reasoning entries to inherit parent settings. The JSON is advisory,
not a runtime model resolver and cannot automatically recover a rejected model name.

JEV sits before this escalation path only for bounded uncertain judgments. It does not
replace coding models or decide architecture. There is no live token-abort threshold.
