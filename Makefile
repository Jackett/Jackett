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

# Finished with `$(shell)`, echo recipe commands going forward
.SHELLFLAGS+= -x


### Top-level targets:

.PHONY: all
## The default target.
all: build

.PHONY: build
## Set up everything for development from this checkout.
build:

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
