# Development, build, and maintenance tasks:

# Defensive settings for make:
#     https://tech.davis-hansson.com/p/make/
SHELL:=bash
.ONESHELL:
.SHELLFLAGS:=-eu -o pipefail -c
.SILENT:
.DELETE_ON_ERROR:
MAKEFLAGS+=--warn-undefined-variables
MAKEFLAGS+=--no-builtin-rules
PS1?=$$

# Project-specific variables:
# Hard link a definition from `./src/Jackett.Common/Definitions/*.yml` into
# `./.compose/prowlarr/config/Definitions/Custom/` to test changes under Prowlarr:
CUSTOM_DEFINITIONS=$(wildcard ./.compose/prowlarr/config/Definitions/Custom/*)

# Finished with `$(shell)`, echo recipe commands going forward
.SHELLFLAGS+= -x


### Top-level targets:

.PHONY: all
## The default target.
all: build

.PHONY: build
## Set up everything for development from this checkout.
build: $(CUSTOM_DEFINITIONS)

.PHONY: start
## Run the local development end-to-end stack services in the background as daemons.
start: build
	docker compose up --pull="always" -d

.PHONY: run
## Run the local development end-to-end stack services in the foreground for debugging.
run: build
	$(MAKE) start
# Scrollback to the container start on a fresh start:
	docker compose logs -ft --tail="100"


### Real Targets:

# When VCS updates the `./src/Jackett.Common/Definitions/*.yml` definition, it breaks
# the hard link. Preserve local customizations, hard link the custom definition over the
# definition in VCS rather than the reverse:
define custom_definition_template=
./$(1): ./$(1:./.compose/prowlarr/config/Definitions/Custom/%=./src/Jackett.Common/Definitions/%)
	git diff --exit-code "$$(<)"
	docker compose stop "prowlarr"
	cp -alfv "$$(@)" "$$(<)"
endef
$(foreach custom_definition,$(CUSTOM_DEFINITIONS),\
    $(eval $(call custom_definition_template,$(custom_definition))))
