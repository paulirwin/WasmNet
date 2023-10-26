;; invoke: working-hard
;; expect: (i32:42)

(module
    (func (export "working-hard") (result i32) 
        nop
        nop
        nop
        nop
        nop
        nop
        i32.const 42
    )
)