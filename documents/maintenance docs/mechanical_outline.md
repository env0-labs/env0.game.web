# Maintenance Mode — Player Experience (Plain Language)

## Initial State

The player is dropped into an empty screen.

There is no prompt explaining what to do.

---

## Valid Input

Only two inputs are accepted:

- `Process`
- `Status`

Any other input:
- generates an error
- informs the player that the only acceptable inputs are `Process` and `Status`

No other commands exist in this mode.

---

## Status

When the player enters `Status`:
- the system displays the work that remains to be completed
- this is shown as a number of **blocks** requiring processing

Status does not change the system.  
It only reports the current state.

---

## Process

When the player enters `Process`:
- one **block** is processed
- input is locked while processing occurs
- the system outputs processing messages
- when processing completes, input control is returned

After completion:
- the processed block is reflected in `Status`

---

## Block Completion

The player continues to process blocks one at a time.

When enough blocks are processed:
- a **container** is marked as complete
- the block count resets
- a new set of blocks becomes available for processing

This change is only visible through `Status`.

---

## Container Completion

As the player completes containers:
- the container completion count increases
- processing continues as normal

There is no change to available commands.

---

## Batching

Once a defined number of containers are completed:
- the player is asked whether they wish to **batch** their work

If the player agrees to batch:
- the container completion count is reset
- batching progress is incremented
- processing continues with new containers

Batching is optional when offered, but once completed it cannot be repeated for the same containers.

---

## Batch Completion

The player repeats this cycle:
- process blocks
- complete containers
- batch work

When a defined number of batches are completed:
- a new option becomes available: `Exit`

---

## Exit

When the player selects `Exit`:
- Maintenance Mode ends
- the player is moved into the **Records** context

No summary is provided.  
No interpretation is given.

---

## One-Sentence Summary

The player processes blocks, blocks complete containers, containers can be batched, and once enough batches are completed the player is allowed to exit Maintenance Mode.
