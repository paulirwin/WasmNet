;; invoke: geu
;; expect: (i32:0)

(module
    (func (export "geu") (result i32)
        i32.const 10
        i32.const 42
        i32.ge_u
    )
)