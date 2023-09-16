;; invoke: and
;; expect: (i32:8)

(module
    (func (export "and") (result i32)
        i32.const 42
        i32.const 8
        i32.and
    )
)