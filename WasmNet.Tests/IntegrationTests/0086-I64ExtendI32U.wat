;; invoke: extend
;; expect: (i64:4294967288)

(module
    (memory 1)
    (func (export "extend") (result i64)
        i32.const -8
        i64.extend_i32_u
    )
)