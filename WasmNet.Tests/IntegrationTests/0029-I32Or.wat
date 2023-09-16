;; invoke: or
;; expect: (i32:1002)

(module
    (func (export "or") (result i32)
        i32.const 42
        i32.const 1000
        i32.or
    )
)