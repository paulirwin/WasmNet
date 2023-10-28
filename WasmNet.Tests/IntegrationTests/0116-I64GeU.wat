;; invoke: geu
;; expect: (i32:1)

(module
    (func (export "geu") (result i32)
        i64.const 42
        i64.const 42
        i64.ge_u
    )
)