;; invoke: proc-exit
;; exit_code: 1

(module
    (type $t0 (func (param i32)))
    (import "wasi_snapshot_preview1" "proc_exit" (func $exit (type $t0)))
    (func (export "proc-exit")
        (call $exit (i32.const 1))
    )
    (memory (;0;) 256 256)
    (export "memory" (memory 0))
)